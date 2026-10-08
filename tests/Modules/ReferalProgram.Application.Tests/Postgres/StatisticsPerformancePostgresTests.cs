using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Npgsql;
using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [PlacementPerformanceFact]
    public async Task Measure_statistics_invite_subtrees()
    {
        var output = Environment.GetEnvironmentVariable("ACTIVITY_TEST_PERF_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "cryptostyle-statistics-perf");
        Directory.CreateDirectory(output);
        await using var db = await Database.Create();
        await db.MeasureStatistics(output);
    }

    private sealed partial class Database
    {
        public async Task MeasureStatistics(string output)
        {
            await Sql("""
                CREATE UNIQUE INDEX ux_places_identity_nulls_not_distinct
                    ON places(marketing_addr,structure_number,profile_addr,place_number) NULLS NOT DISTINCT;
                CREATE INDEX idx_places_profile_matrix_filling
                    ON places(marketing_addr,structure_number,profile_addr,matrix_filling,place_number);
                CREATE INDEX idx_places_search_index
                    ON places(marketing_addr,structure_number,lower("index") text_pattern_ops);
                CREATE INDEX idx_places_profile_structures
                    ON places(marketing_addr,profile_addr,structure_number) WHERE structure_number<>0;
                INSERT INTO structures SELECT 'perf',n,0,0,0,1,false,'{}',NULL,NULL FROM generate_series(0,8) n;
                """);
            var cs = new NpgsqlConnectionStringBuilder(data.ConnectionString)
            { Password = "activity-test-only", Pooling = true, CommandTimeout = 180 }.ConnectionString;
            await using var measured = NpgsqlDataSource.Create(cs);
            await using var connection = await measured.OpenConnectionAsync();
            var queries = new ProgramStatisticsQueries(measured);
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "backend-netcore.sln"))) root = root.Parent;
            var source = await File.ReadAllTextAsync(Path.Combine(root!.FullName,
                "src/Modules/ReferalProgram/ReferalProgram.Infrastructure/Queries/ProgramStatisticsQueries.cs"));
            var sql = source.Split("\"\"\"")[1];
            var results = new List<object>();
            string Profile(int n) => "0:" + n.ToString("x64");
            foreach (var totalRows in new[] { 100_000, 1_000_000, 3_000_000 })
            {
                await connection.ExecuteAsync("TRUNCATE places RESTART IDENTITY CASCADE");
                // Balanced binary invite tree; one invite plus three places in each of eight structures.
                // Paid placement deliberately differs from invitation membership.
                await connection.ExecuteAsync("""
                    CREATE TEMP TABLE profiles AS
                    WITH RECURSIVE tree AS (
                        SELECT 1 AS n, '00000001'::text AS mp, 1 AS depth
                        UNION ALL
                        SELECT child.n, tree.mp || lpad(to_hex(child.n%2+1),8,'0'), depth+1
                        FROM tree CROSS JOIN LATERAL (VALUES(tree.n*2),(tree.n*2+1)) child(n)
                        WHERE child.n<=@count
                    ) SELECT * FROM tree;
                    INSERT INTO places(id,parent_id,mp,pos_group,marketing_addr,structure_number,
                        profile_addr,place_number,profile_login,"index",parent_profile_addr,parent_profile_login,
                        parent_place_number,created_at,activated_at,is_active,kind,pos,filling,deep,matrix_filling)
                    SELECT (p.n-1)*25+slot+1,
                        CASE WHEN slot=0 AND p.n>1 THEN (p.n/2-1)*25+1 ELSE NULL END,
                        CASE WHEN slot=0 THEN p.mp ELSE lpad(to_hex((p.n-1)*25+slot+1),8,'0') END,
                        0,'perf',CASE WHEN slot=0 THEN 0 ELSE (slot-1)/3+1 END,
                        '0:'||lpad(to_hex(p.n),64,'0'),CASE WHEN slot=0 THEN 1 ELSE (slot-1)%3+1 END,
                        'p'||p.n,'p'||p.n||'-'||slot,
                        CASE WHEN slot=0 AND p.n>1 THEN '0:'||lpad(to_hex(p.n/2),64,'0') ELSE NULL END,
                        NULL,CASE WHEN slot=0 AND p.n>1 THEN 1 ELSE NULL END,
                        1,CASE WHEN (p.n+slot)%3<>0 THEN 1 ELSE NULL END,(p.n+slot)%4=0,
                        CASE WHEN slot=0 THEN 0 ELSE (slot-1)%3 END,
                        1,0,CASE WHEN slot=0 THEN p.depth ELSE 1 END,1
                    FROM profiles p CROSS JOIN generate_series(0,24) slot;
                    DROP TABLE profiles;
                    """,new { count=totalRows/25 }, commandTimeout:180);
                await connection.ExecuteAsync("VACUUM (ANALYZE) places",commandTimeout:180);
                foreach (var profile in new[] { totalRows/25, 1000, 4, 1 })
                {
                    var profileAddr = Profile(profile);
                    var expected = 0;
                    for (long start=profile*2, end=profile*2+1; start<=totalRows/25; start*=2,end=end*2+1)
                        expected += (int)(Math.Min(end,totalRows/25)-start+1);
                    var cold = Stopwatch.StartNew();
                    var result = await queries.GetAsync("perf",profileAddr,default);
                    var firstMs = cold.Elapsed.TotalMilliseconds;
                    Assert.NotNull(result);
                    Assert.All(result.Structures,s => {
                        Assert.Equal(expected,s.TotalProfiles);
                        Assert.Equal(expected*(s.StructureNumber==0?1:3),s.TotalPlaces);
                    });
                    for(var i=0;i<2;i++) await queries.GetAsync("perf",profileAddr,default);
                    var samples = new List<double>();
                    for(var i=0;i<10;i++) {
                        var watch=Stopwatch.StartNew();
                        await queries.GetAsync("perf",profileAddr,default);
                        samples.Add(watch.Elapsed.TotalMilliseconds);
                    }
                    var plans = new List<JsonElement>();
                    foreach(var statement in sql.Split(';',StringSplitOptions.RemoveEmptyEntries).Where(s=>!string.IsNullOrWhiteSpace(s))) {
                        var plan = await connection.QuerySingleAsync<string>("EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) "+statement,
                            new {marketingAddr="perf",profileAddr},commandTimeout:180);
                        plans.Add(JsonSerializer.Deserialize<JsonElement>(plan));
                    }
                    await File.WriteAllTextAsync(Path.Combine(output,$"{totalRows}-{profile}-plans.json"),JsonSerializer.Serialize(plans));
                    results.Add(new { totalRows, profiles=totalRows/25, profile, descendants=expected, firstMs,
                        mean=samples.Average(), median=samples.Order().ElementAt(5), p95=samples.Max(), samples });
                    await File.WriteAllTextAsync(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
                }
            }
        }
    }
}
