using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed class PlacementPerformanceFactAttribute : FactAttribute
{
    public PlacementPerformanceFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("ACTIVITY_TEST_PERF") != "1"
            || Environment.GetEnvironmentVariable("ACTIVITY_TEST_POSTGRES_PORT") is null)
            Skip = "Opt-in performance measurement on disposable PostgreSQL only.";
    }
}

public sealed partial class ActivityPostgresTests
{
    [PlacementPerformanceFact]
    public async Task Measure_placement_query_plans_on_synthetic_trees()
    {
        var output = Environment.GetEnvironmentVariable("ACTIVITY_TEST_PERF_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "cryptostyle-placement-perf");
        Directory.CreateDirectory(output);
        await using var db = await Database.Create();
        await db.MeasurePlacement(output);
    }

    [PlacementPerformanceFact]
    public async Task Measure_invite_lookup_and_tree_batch_plans()
    {
        var output = Environment.GetEnvironmentVariable("ACTIVITY_TEST_PERF_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "cryptostyle-placement-perf");
        Directory.CreateDirectory(output);
        await using var db = await Database.Create();
        await db.MeasureInviteQueries(output);
    }

    [PlacementPerformanceFact]
    public async Task Measure_noncorrelated_invite_filter_alternative()
    {
        var output = Environment.GetEnvironmentVariable("ACTIVITY_TEST_PERF_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "cryptostyle-placement-perf");
        Directory.CreateDirectory(output);
        await using var db = await Database.Create();
        await db.MeasureAlternative(output);
    }

    private sealed partial class Database
    {
        public async Task MeasurePlacement(string output)
        {
            // Known current index definitions; the historical active_mp definition is
            // not present in the repository and is deliberately not guessed here.
            await Sql("""
                CREATE UNIQUE INDEX ux_places_identity_nulls_not_distinct
                  ON places (marketing_addr,structure_number,profile_addr,place_number) NULLS NOT DISTINCT;
                CREATE INDEX idx_places_profile_matrix_filling
                  ON places (marketing_addr,structure_number,profile_addr,matrix_filling,place_number);
                """);
            var capture = new CommandCapture();
            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
            var cs = new NpgsqlConnectionStringBuilder(data.ConnectionString)
            {
                Password = "activity-test-only", Options = "-c statement_timeout=5000", CommandTimeout = 10
            }.ConnectionString;
            var builder = new NpgsqlDataSourceBuilder(cs);
            builder.UseLoggerFactory(loggerFactory);
            builder.EnableParameterLogging();
            await using var measured = builder.Build();
            var queries = new PlaceQueries(measured);
            var results = new List<object>();
            foreach (var count in new[] { 10000, 100000 })
            {
                await SeedPerformance(count);
                foreach (var additionalIndex in new[] { false, true })
                {
                    if (additionalIndex)
                    {
                        await Sql("CREATE INDEX perf_active_invites ON places (marketing_addr,profile_addr) WHERE structure_number=0 AND place_number=1 AND is_active=true");
                        await Sql("ANALYZE places");
                    }
                    foreach (var subtree in new[] { 1, 64 })
                    {
                        await using var connection = await measured.OpenConnectionAsync();
                        var rootMp = await connection.QuerySingleAsync<string>("SELECT mp FROM places WHERE id=@id", new { id = 1000000 + subtree });
                        var scopedCount = await connection.ExecuteScalarAsync<long>("SELECT count(*) FROM places WHERE structure_number=1 AND mp LIKE @prefix", new { prefix = rootMp + "%" });
                        foreach (var mode in new[] { "legacy", "own", "invite", "combined" })
                        {
                            PlacementActivityRules? activity = mode == "legacy" ? null : new(
                                "p" + count, "p" + count / 2, mode is "own" or "combined",
                                mode == "combined", mode is "invite" or "combined", false);
                            var calls = new Dictionary<string, Func<Task>>
                            {
                                ["classic"] = async () => { await queries.GetOpenPlacesByMpPrefixAsync("perf",1,rootMp,2,1,50,default,activity); },
                                ["chess"] = async () => { await queries.GetUnfilledPlacesInDepthWindowAsync("perf",1,rootMp,2,2,[],default,activity); },
                                ["radar"] = async () => { await queries.GetFirstActiveUnfilledPlaceAsync("perf",1,rootMp,2,true,2,[],default,activity); },
                                ["profile_frontier"] = async () => { await queries.GetProfileFrontierCandidateAsync("perf",1,rootMp,2,35,[],default,activity); },
                                ["system_gap"] = async () => { await queries.GetSystemGapCandidateAsync("perf",1,rootMp,2,[],default,activity); }
                            };
                            foreach (var (algorithm, invoke) in calls)
                            {
                                capture.CommandText = null;
                                var watch = Stopwatch.StartNew();
                                bool timeout = false;
                                try { await invoke(); }
                                catch (PostgresException e) when (e.SqlState == "57014") { timeout = true; }
                                watch.Stop();
                                var sql = capture.CommandText ?? throw new InvalidOperationException("Npgsql command text was not captured.");
                                var key = $"{count}-{subtree}-{mode}-{algorithm}-{(additionalIndex ? "partial-index" : "known-indexes")}";
                                await File.WriteAllTextAsync(Path.Combine(output,key + ".sql"),sql);
                                var parameters = capture.Parameters?.ToArray()
                                    ?? throw new InvalidOperationException("Npgsql parameters were not captured.");
                                var times = new List<double>();
                                if (!timeout)
                                {
                                    for (var run=0; run<3; run++)
                                    {
                                        try
                                        {
                                            var plan = await Explain(connection, "EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + sql, parameters);
                                            using var json = JsonDocument.Parse(plan);
                                            times.Add(json.RootElement[0].GetProperty("Execution Time").GetDouble());
                                            await File.WriteAllTextAsync(Path.Combine(output,key + $"-{run}.json"),plan);
                                        }
                                        catch (PostgresException e) when (e.SqlState == "57014") { timeout = true; break; }
                                    }
                                }
                                if (timeout)
                                {
                                    var plan = await Explain(connection, "EXPLAIN (FORMAT JSON) " + sql, parameters);
                                    await File.WriteAllTextAsync(Path.Combine(output,key + "-estimated.json"),plan);
                                }
                                results.Add(new { count, totalRows = count*2, scopedCount, subtree, mode, algorithm, additionalIndex,
                                    timeout, wallMs = watch.Elapsed.TotalMilliseconds,
                                    executionMs = times, medianMs = times.Count == 0 ? (double?)null : times.Order().ElementAt(times.Count/2) });
                                await File.WriteAllTextAsync(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));
                            }
                        }
                    }
                    if (additionalIndex) await Sql("DROP INDEX perf_active_invites");
                }
            }
        }

        public async Task MeasureInviteQueries(string output)
        {
            await Sql("CREATE UNIQUE INDEX ux_places_identity_nulls_not_distinct ON places (marketing_addr,structure_number,profile_addr,place_number) NULLS NOT DISTINCT");
            await SeedPerformance(100000);
            var capture = new CommandCapture();
            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
            var builder = new NpgsqlDataSourceBuilder(new NpgsqlConnectionStringBuilder(data.ConnectionString)
                { Password = "activity-test-only" }.ConnectionString);
            builder.UseLoggerFactory(loggerFactory);
            builder.EnableParameterLogging();
            await using var measured = builder.Build();
            var queries = new PlaceQueries(measured);
            var results = new List<object>();
            foreach (var additionalIndex in new[] { false, true })
            {
                if (additionalIndex)
                {
                    await Sql("CREATE INDEX perf_active_invites ON places (marketing_addr,profile_addr) WHERE structure_number=0 AND place_number=1 AND is_active=true");
                    await Sql("ANALYZE places");
                }
                foreach (var batch in new[] { 1, 100, 1000 })
                {
                    var profiles = Enumerable.Range(1,batch).Select(n => "p" + n*97).ToArray();
                    if (batch == 1) await queries.GetPlaceAsync("perf",0,profiles[0],1,default);
                    else await queries.GetActiveInviteProfilesAsync("perf",profiles,default);
                    var sql = capture.CommandText!;
                    var parameters = capture.Parameters!.ToArray();
                    if (batch > 1) parameters[1] = profiles;
                    await using var connection = await measured.OpenConnectionAsync();
                    var times = new List<double>();
                    for (var run=0; run<3; run++)
                    {
                        var plan = await Explain(connection,"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + sql,parameters);
                        using var json = JsonDocument.Parse(plan);
                        times.Add(json.RootElement[0].GetProperty("Execution Time").GetDouble());
                        await File.WriteAllTextAsync(Path.Combine(output,$"aux-{batch}-{additionalIndex}-{run}.json"),plan);
                    }
                    results.Add(new { batch, additionalIndex, executionMs=times, medianMs=times.Order().ElementAt(1) });
                }
            }
            await File.WriteAllTextAsync(Path.Combine(output,"auxiliary.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));
        }

        public async Task MeasureAlternative(string output)
        {
            await Sql("CREATE UNIQUE INDEX ux_places_identity_nulls_not_distinct ON places (marketing_addr,structure_number,profile_addr,place_number) NULLS NOT DISTINCT");
            await Sql("CREATE INDEX idx_places_profile_matrix_filling ON places (marketing_addr,structure_number,profile_addr,matrix_filling,place_number)");
            await SeedPerformance(100000);
            var capture = new CommandCapture();
            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
            var builder = new NpgsqlDataSourceBuilder(new NpgsqlConnectionStringBuilder(data.ConnectionString)
                { Password = "activity-test-only" }.ConnectionString);
            builder.UseLoggerFactory(loggerFactory);
            builder.EnableParameterLogging();
            await using var measured = builder.Build();
            var queries = new PlaceQueries(measured);
            var rules = new PlacementActivityRules("p100000","p50000",false,false,true,false);
            var calls = new Dictionary<string,Func<Task>>
            {
                ["classic"] = async () => { await queries.GetOpenPlacesByMpPrefixAsync("perf",1,"00000001",2,1,50,default,rules); },
                ["chess"] = async () => { await queries.GetUnfilledPlacesInDepthWindowAsync("perf",1,"00000001",2,2,[],default,rules); },
                ["radar"] = async () => { await queries.GetFirstActiveUnfilledPlaceAsync("perf",1,"00000001",2,true,2,[],default,rules); },
                ["system_gap"] = async () => { await queries.GetSystemGapCandidateAsync("perf",1,"00000001",2,[],default,rules); }
            };
            var results = new List<object>();
            foreach (var additionalIndex in new[] { false,true })
            {
                if (additionalIndex)
                {
                    await Sql("CREATE INDEX perf_active_invites ON places (marketing_addr,profile_addr) WHERE structure_number=0 AND place_number=1 AND is_active=true");
                    await Sql("ANALYZE places");
                }
                foreach (var (algorithm,invoke) in calls)
                {
                    await invoke();
                    var sql = capture.CommandText!.TrimEnd().TrimEnd(';');
                    var parameters = capture.Parameters!.ToArray();
                    var alternative = System.Text.RegularExpressions.Regex.Replace(sql,
                        @"EXISTS \(\s*SELECT 1 FROM public\.places activity_invite\s*WHERE activity_invite\.marketing_addr = (\w+)\.marketing_addr\s*AND activity_invite\.structure_number = 0\s*AND activity_invite\.place_number = 1\s*AND activity_invite\.profile_addr = \1\.profile_addr\s*AND activity_invite\.is_active = true\s*\)",
                        m => $"{m.Groups[1].Value}.profile_addr IN (SELECT profile_addr FROM public.places WHERE marketing_addr=$1 AND structure_number=0 AND place_number=1 AND is_active=true)");
                    if (sql == alternative)
                    {
                        // Production now uses membership. Reconstruct the former EXISTS
                        // only for the diagnostic comparison, keeping the same parameters.
                        sql = System.Text.RegularExpressions.Regex.Replace(alternative,
                            @"(\w+)\.profile_addr IN \(\s*SELECT activity_invite\.profile_addr FROM public\.places activity_invite\s*WHERE activity_invite\.marketing_addr = \$1\s*AND activity_invite\.structure_number = 0\s*AND activity_invite\.place_number = 1\s*AND activity_invite\.is_active = true\s*\)",
                            m => $"EXISTS (SELECT 1 FROM public.places activity_invite WHERE activity_invite.marketing_addr={m.Groups[1].Value}.marketing_addr AND activity_invite.structure_number=0 AND activity_invite.place_number=1 AND activity_invite.profile_addr={m.Groups[1].Value}.profile_addr AND activity_invite.is_active=true)");
                    }
                    Assert.NotEqual(sql,alternative);
                    await using var connection = await measured.OpenConnectionAsync();
                    var compare = $"WITH expected AS ({sql}), alternative AS ({alternative}) SELECT NOT EXISTS ((SELECT * FROM expected EXCEPT ALL SELECT * FROM alternative) UNION ALL (SELECT * FROM alternative EXCEPT ALL SELECT * FROM expected))";
                    await using (var command = BoundCommand(connection,compare,parameters))
                        Assert.True((bool)(await command.ExecuteScalarAsync())!);
                    foreach (var variant in new[] { "exists", "membership" })
                    {
                        var query = variant == "exists" ? sql : alternative;
                        var times = new List<double>();
                        for (var run=0;run<3;run++)
                        {
                            var plan = await Explain(connection,"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + query,parameters);
                            using var json = JsonDocument.Parse(plan);
                            times.Add(json.RootElement[0].GetProperty("Execution Time").GetDouble());
                            await File.WriteAllTextAsync(Path.Combine(output,$"alternative-{algorithm}-{additionalIndex}-{variant}-{run}.json"),plan);
                        }
                        results.Add(new { algorithm,additionalIndex,variant,executionMs=times,medianMs=times.Order().ElementAt(1) });
                    }
                }
            }
            await File.WriteAllTextAsync(Path.Combine(output,"alternatives.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));
        }

        private static async Task<string> Explain(NpgsqlConnection connection, string sql, object?[] values)
        {
            await using var command = BoundCommand(connection, sql, values);
            return (string)(await command.ExecuteScalarAsync())!;
        }

        private static NpgsqlCommand BoundCommand(NpgsqlConnection connection, string sql, object?[] values)
        {
            var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 10 };
            for (var i = 0; i < values.Length; i++)
            {
                // Diagnostic logging formats arrays; placement lock lists are empty here.
                object value = sql.Contains($"unnest(${i+1})", StringComparison.Ordinal)
                    ? Array.Empty<string>() : values[i] ?? DBNull.Value;
                command.Parameters.Add(new NpgsqlParameter { Value = value });
            }
            return command;
        }

        private async Task SeedPerformance(int count)
        {
            await Sql("TRUNCATE marketing_tasks, places, profile_volumes RESTART IDENTITY CASCADE");
            await Sql("""
                WITH RECURSIVE tree AS (
                    SELECT 1 AS n, '00000001'::text AS mp, 1 AS deep
                    UNION ALL
                    SELECT child.n, tree.mp || lpad(to_hex(child.n % 2 + 1),8,'0'), tree.deep+1
                    FROM tree CROSS JOIN LATERAL (VALUES(tree.n*2),(tree.n*2+1)) child(n)
                    WHERE child.n <= @count
                )
                INSERT INTO places (id,parent_id,mp,pos_group,marketing_addr,structure_number,
                    profile_addr,place_number,profile_login,"index",parent_profile_addr,parent_profile_login,
                    parent_place_number,created_at,activated_at,is_active,kind,pos,filling,deep,matrix_filling)
                SELECT n + s.number*1000000, CASE WHEN n=1 THEN NULL ELSE n/2 + s.number*1000000 END,
                    mp,0,'perf',s.number,'p'||n,1,'p'||n,'p'||n||'-1',
                    CASE WHEN n=1 THEN NULL ELSE 'p'||(n/2) END,
                    CASE WHEN n=1 THEN NULL ELSE 'p'||(n/2) END,
                    CASE WHEN n=1 THEN NULL ELSE 1 END,1,n,
                    CASE WHEN s.number=0 THEN n%3<>0 ELSE n%5<>0 END,
                    0,CASE WHEN n=1 THEN 0 ELSE n%2+1 END,
                    greatest(0,least(2,@count-n*2+1)),deep,1
                FROM tree CROSS JOIN (VALUES(0),(1)) s(number)
                """,new { count });
            await Sql("VACUUM (ANALYZE) places");
        }
    }

    private sealed class CommandCapture : ILoggerProvider, ILogger
    {
        public string? CommandText { get; set; }
        public object?[]? Parameters { get; set; }
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Dispose() { }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
                foreach (var value in values)
                {
                    if (value.Key == "CommandText" && value.Value is string sql) CommandText=sql;
                    if (value.Key == "Parameters" && value.Value is object?[] parameters) Parameters = parameters;
                }
        }
    }
}
