using System.Text.RegularExpressions;
using Dapper;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Optimized_frontier_matches_frozen_query_across_topology_activity_width_limits_and_locks()
    {
        await using var db = await Database.Create();
        var comparisons = 0;
        foreach (var size in new[] { 63, 127, 255 })
        {
            await db.SeedFrontierComparison(size);
            foreach (var rootNumber in new[] { 1, 4 })
            foreach (byte width in new byte[] { 0, 2, 3 })
            foreach (uint limit in new uint[] { 1, 5, 35 })
            foreach (var locked in new[] { false, true })
            foreach (var mode in new[] { "legacy", "own", "spill", "combined" })
            {
                var root = (await db.Places.GetPlaceAsync(1000000 + rootNumber, default))!;
                PlacementActivityRules? rules = mode == "legacy" ? null : new(
                    "p" + (size-1), "p" + (size-1)/2, mode is "own" or "combined",
                    mode is "spill" or "combined", false);
                string[] locks = locked ? [root.Mp + "00000001"] : [];
                var before = await db.FrontierBefore(root.Mp, width, limit, locks, rules);
                var after = await db.Places.GetProfileFrontierCandidateAsync("perf",1,root.Mp,width,limit,locks,default,rules);
                Assert.True(before?.Id == after?.Id,
                    $"size={size} root={rootNumber} width={width} limit={limit} lock={locked} mode={mode}: old={before?.Id}, new={after?.Id}");
                comparisons++;
            }
        }
        Assert.Equal(432, comparisons);
    }

    [DockerPostgresFact]
    public async Task Indexed_identity_lookup_preserves_system_null_whitespace_and_program_isolation()
    {
        await using var db = await Database.Create();
        await db.SeedFrontierComparison(63);
        foreach (var profile in new string?[] { null, "", "   " })
            Assert.Equal(1000011, (await db.Places.GetPlaceAsync("perf",1,profile,11,default))?.Id);
        Assert.Null(await db.Places.GetPlaceAsync("other-program",1,null,11,default));
        Assert.Equal(1000001, (await db.Places.GetPlaceAsync("perf",1,"p1",1,default))?.Id);
        Assert.Equal(1, (await db.Places.GetPlaceAsync("perf",0,"p1",1,default))?.Id);
        Assert.Null(await db.Places.GetPlaceAsync("perf",1,"p1",2,default));
        Assert.Null(await db.Places.GetPlaceAsync("perf",1,"absent",1,default));
    }

    private sealed partial class Database
    {
        public async Task SeedFrontierComparison(int size)
        {
            await SeedPerformance(size);
            await Sql("""
                UPDATE places SET profile_addr=NULL, profile_login=NULL, place_number=id%1000000
                WHERE structure_number=1 AND id%11=1;
                UPDATE places SET kind=2 WHERE structure_number=1 AND id%7=0;
                UPDATE places SET is_active=false WHERE structure_number=1 AND id%13=0;
                """);
            // Ensure the null-identity regression case is deterministic.
            await Sql("UPDATE places SET profile_addr=NULL, profile_login=NULL, place_number=11 WHERE id=1000011");
        }

        public async Task<PlaceResponse?> FrontierBefore(string rootMp, byte width, uint limit,
            string[] locks, PlacementActivityRules? activity)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            const string relative = "tests/Modules/ReferalProgram.Application.Tests/Postgres/Fixtures/profile_frontier_before_optimization.sql";
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName,relative))) directory=directory.Parent;
            Assert.NotNull(directory);
            var sql = await File.ReadAllTextAsync(Path.Combine(directory.FullName,relative));
            // Compare the frozen frontier topology with supported own/spillover permissions.
            if (activity?.ChangesAutomaticEligibility == true)
                sql = Regex.Replace(sql,@"\b(?:(scoped|level_place)\.)?is_active = true",m =>
                {
                    var alias = m.Groups[1].Success ? m.Groups[1].Value : "places";
                    var own = $"({alias}.profile_addr IS NOT NULL AND @activityChild IS NOT NULL AND ({alias}.profile_addr=@activityChild OR {alias}.profile_addr=@activityInviter))";
                    return $"({alias}.is_active=true OR CASE WHEN {own} THEN @allowOwn ELSE @allowSpillover END)";
                });
            var parameters = new DynamicParameters(new { marketingAddr="perf", structureNumber=(short)1,
                rootMp, mpPrefix=rootMp+"%", width=(long)width, profiledWidthLimit=(long)limit, lockMps=locks,
                allowOwn=activity?.AllowOwnChildren ?? false, allowSpillover=activity?.AllowInactiveSpillover ?? false });
            parameters.Add("activityChild",activity?.ChildProfileAddr,System.Data.DbType.String);
            parameters.Add("activityInviter",activity?.InviterProfileAddr,System.Data.DbType.String);
            await using var connection = await data.OpenConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(sql,parameters);
        }
    }
}
