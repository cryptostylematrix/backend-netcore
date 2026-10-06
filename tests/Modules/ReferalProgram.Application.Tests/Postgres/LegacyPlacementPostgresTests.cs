using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services.PositionStrategies;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    private static IPositionAlgorithmStrategy[] Algorithms(IPositionCandidateQueries queries) =>
    [
        new ClassicPositionAlgorithmStrategy(queries), new TrimmedClassicPositionAlgorithmStrategy(queries),
        new EmptyParentPositionAlgorithmStrategy(queries), new ChessPositionAlgorithmStrategy(queries),
        new RadarPositionAlgorithmStrategy(queries), new ProfileFrontierPositionAlgorithmStrategy(queries),
        new SystemGapPositionAlgorithmStrategy(queries)
    ];

    [DockerPostgresFact]
    public async Task Legacy_placement_snapshots_cover_ordering_depth_locks_and_system_priority()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "owner", null, false);
        await db.InsertPlace(5, "program", 1, "inviter", 4, true);
        await db.InsertPlace(6, "program", 1, "member", 4, true);
        await db.InsertPlace(7, "program", 1, "system", 5, true);
        await db.InsertPlace(8, "program", 1, "extra", 6, true);
        await db.Sql("UPDATE places SET profile_addr=NULL, profile_login=NULL WHERE id=7; UPDATE places SET activated_at=id*10 WHERE id>=4");
        var root = (await db.Places.GetPlaceAsync("program", 1, "owner", 1, default))!;
        var snapshots = new List<object>();
        foreach (var active in new[] { false, true })
        foreach (var prioritize in new[] { false, true })
        foreach (byte spread in new byte[] { 1, 2, 3 })
        foreach (var locked in new[] { false, true })
        {
            await db.Sql("UPDATE places SET is_active=@active WHERE id=5", new { active });
            string[] locks = locked ? [root.Mp + "00000002"] : [];
            var context = new PositionAlgorithmStrategyContext("program", 1, 3, root, 0, prioritize, spread, locks, 2, 10);
            foreach (var algorithm in Algorithms(db.Places))
            {
                var position = await algorithm.FindNextAsync(context, default);
                if (position is not null)
                {
                    Assert.NotEqual("owner", position.ProfileAddr); // inactive root is never a candidate
                    if (!active) Assert.NotEqual("inviter", position.ProfileAddr);
                    if (locked) Assert.False(position.Mp.StartsWith(locks[0], StringComparison.Ordinal));
                }
                snapshots.Add(new { algorithm.Name, active, prioritize, spread, locked,
                    ProfileAddr = position?.ProfileAddr, PlaceNumber = position?.PlaceNumber,
                    Pos = position?.Pos, Mp = position?.Mp, PosGroup = position?.PosGroup });
            }
        }
        var path = Environment.GetEnvironmentVariable("ACTIVITY_TEST_SNAPSHOT_PATH");
        if (path is not null)
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal(168, snapshots.Count);
    }
}
