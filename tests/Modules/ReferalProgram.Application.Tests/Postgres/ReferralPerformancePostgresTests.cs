using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Npgsql;
using ReferalProgram.Application.Features.Invites;
using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [PlacementPerformanceFact]
    public async Task Measure_referral_structure_memberships()
    {
        var output = Environment.GetEnvironmentVariable("ACTIVITY_TEST_PERF_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "cryptostyle-referral-perf");
        Directory.CreateDirectory(output);
        await using var db = await Database.Create();
        await db.MeasureReferralMemberships(output);
    }

    private sealed partial class Database
    {
        public async Task MeasureReferralMemberships(string output)
        {
            // Only indexes whose definitions are available in the repository.
            await Sql("""
                CREATE UNIQUE INDEX ux_places_identity_nulls_not_distinct
                  ON places (marketing_addr,structure_number,profile_addr,place_number) NULLS NOT DISTINCT;
                CREATE INDEX idx_places_profile_matrix_filling
                  ON places (marketing_addr,structure_number,profile_addr,matrix_filling,place_number);
                CREATE INDEX idx_places_search_index
                  ON places (marketing_addr,structure_number,lower("index") text_pattern_ops);
                """);
            var cs = new NpgsqlConnectionStringBuilder(data.ConnectionString)
            {
                Password = "activity-test-only", Pooling = true, CommandTimeout = 180
            }.ConnectionString;
            await using var measured = NpgsqlDataSource.Create(cs);
            var queries = new PlaceQueries(measured);
            var handler = new GetReferralsQueryHandler(queries);
            var results = new List<object>();
            string Profile(int n) => "0:" + n.ToString("x64");
            foreach (var totalRows in new[] { 100_000, 1_000_000, 3_000_000 })
            {
                await using var connection = await measured.OpenConnectionAsync();
                await connection.ExecuteAsync("TRUNCATE marketing_tasks, places, profile_volumes RESTART IDENTITY CASCADE");
                // Every profile has an invite and three places in each of eight structures.
                // Each curator has 40 direct referrals; page size is the UI default of 20.
                await connection.ExecuteAsync("""
                    INSERT INTO places (id,parent_id,mp,pos_group,marketing_addr,structure_number,
                        profile_addr,place_number,profile_login,"index",parent_profile_addr,parent_profile_login,
                        parent_place_number,created_at,activated_at,is_active,kind,pos,filling,deep,matrix_filling)
                    SELECT n,NULL,lpad(to_hex(n),8,'0'),0,'perf',
                        CASE WHEN slot=0 THEN 0 ELSE (slot-1)/3+1 END,
                        '0:'||lpad(to_hex(profile),64,'0'),CASE WHEN slot=0 THEN 1 ELSE (slot-1)%3+1 END,
                        'p'||profile,'p'||profile||'-'||slot,
                        '0:'||lpad(to_hex((profile-1)/40),64,'0'),'p'||((profile-1)/40),
                        1,1,CASE WHEN n%2=0 THEN 1 ELSE NULL END,n%2=0,0,(profile-1)%40+1,0,1,1
                    FROM generate_series(1,@totalRows) n
                    CROSS JOIN LATERAL (SELECT (n-1)/25+1 AS profile,(n-1)%25 AS slot) x
                    """, new { totalRows }, commandTimeout: 180);
                await connection.ExecuteAsync("VACUUM (ANALYZE) places", commandTimeout: 180);
                foreach (var indexed in new[] { false, true })
                {
                    if (indexed)
                    {
                        await connection.ExecuteAsync("CREATE INDEX perf_memberships ON places (marketing_addr,profile_addr,structure_number) WHERE structure_number<>0", commandTimeout: 180);
                        await connection.ExecuteAsync("ANALYZE places", commandTimeout: 180);
                    }
                    foreach (var page in new[] { 1, 2 })
                    {
                        var parent = Profile(totalRows / 25 / 80);
                        var request = new GetReferralsQuery("perf", parent, page, 20);
                        var partners = (await queries.GetChildrenAsync("perf",0,parent,1,page,20,default)).Items.ToArray();
                        Assert.Equal(20, partners.Length);
                        var profiles = partners.Select(p => p.ProfileAddr!).ToArray();
                        var membership = await queries.GetProfileStructureNumbersAsync("perf",profiles,default);
                        Assert.Equal(20,membership.Count);
                        Assert.All(membership.Values, numbers => Assert.Equal(Enumerable.Range(1,8).Select(n=>(byte)n), numbers));
                        async Task Baseline() {
                            var old = await queries.GetChildrenAsync("perf",0,parent,1,page,20,default);
                            _ = old.Items.Select(p=>p.ToInviteData([])).ToArray();
                        }
                        async Task Current() { var result=await handler.Handle(request,default); _=result.Value.Items.ToArray(); }
                        async Task Membership() { await queries.GetProfileStructureNumbersAsync("perf",profiles,default); }
                        for(var i=0;i<5;i++) { await Baseline(); await Current(); await Membership(); }
                        var baseline=new List<double>(); var current=new List<double>(); var added=new List<double>();
                        async Task Record(Func<Task> action,List<double> target) {
                            var watch=Stopwatch.StartNew(); await action(); target.Add(watch.Elapsed.TotalMilliseconds);
                        }
                        for(var i=0;i<40;i++) {
                            if(i%2==0) { await Record(Baseline,baseline); await Record(Current,current); }
                            else { await Record(Current,current); await Record(Baseline,baseline); }
                            await Record(Membership,added);
                        }
                        object Summary(List<double> samples) => new {
                            mean=samples.Average(), median=samples.Order().ElementAt(samples.Count/2),
                            p95=samples.Order().ElementAt((int)Math.Ceiling(samples.Count*.95)-1), samples
                        };
                        var plan=await connection.QuerySingleAsync<string>("""
                            EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON)
                            SELECT DISTINCT profile_addr,structure_number FROM places
                            WHERE marketing_addr=@marketingAddr AND profile_addr=ANY(@profiles) AND structure_number<>0
                            ORDER BY profile_addr,structure_number
                            """,new {marketingAddr="perf", profiles});
                        await File.WriteAllTextAsync(Path.Combine(output,$"{totalRows}-{indexed}-{page}-plan.json"),plan);
                        results.Add(new { totalRows,profileCount=totalRows/25,page,indexed,iterations=40,
                            baseline=Summary(baseline),current=Summary(current),membership=Summary(added) });
                        await File.WriteAllTextAsync(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions {WriteIndented=true}));
                    }
                    if(indexed) await connection.ExecuteAsync("DROP INDEX perf_memberships");
                }
            }
        }
    }
}
