using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Application.Services.PositionStrategies;
using ReferalProgram.Application.Services.RootStrategies;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Placement_all_algorithms_apply_own_and_spillover_permissions_before_selecting_candidates()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "inviter", null, false);
        foreach (var active in new[] { false, true })
        foreach (var inviteActive in new[] { false, true })
        {
            await db.Sql("UPDATE places SET is_active=@active WHERE id=4; UPDATE places SET is_active=@inviteActive WHERE id=2", new { active, inviteActive });
            var root = (await db.Places.GetPlaceAsync("program", 1, "inviter", 1, default))!;
            foreach (var relationship in new[] { "own", "invited", "spillover", "system" })
            foreach (var own in new[] { false, true })
            foreach (var spill in new[] { false, true })
            foreach (var requireInvite in new[] { false, true })
            {
                var child = relationship switch { "own" => "inviter", "invited" => "member", "system" => null, _ => "unrelated" };
                var rules = new PlacementActivityRules(child, relationship == "invited" ? "inviter" : null, own, spill, requireInvite, false);
                var context = new PositionAlgorithmStrategyContext("program", 1, 3, root, 0, true, 1, [], 2, 10, rules);
                var expected = (active || (relationship is "own" or "invited" ? own : spill))
                    && (relationship is "own" or "invited" || !requireInvite || inviteActive);
                foreach (var algorithm in Algorithms(db.Places))
                {
                    var position = await algorithm.FindNextAsync(context, default);
                    Assert.True((position is not null) == expected,
                        $"{algorithm.Name}: {relationship}, active={active}, invite={inviteActive}, own={own}, spill={spill}, require={requireInvite}");
                    if (position is not null) Assert.Equal("inviter", position.ProfileAddr);
                }
            }
        }
    }

    [DockerPostgresFact]
    public async Task Placement_banned_parent_does_not_hide_eligible_descendants_or_limit_the_depth_window()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.Sql("UPDATE places SET is_active=false WHERE id=1; UPDATE places SET is_active=true WHERE id=2");
        await db.InsertPlace(4, "program", 1, "owner", null, true);
        await db.InsertPlace(5, "program", 1, "inviter", 4, true);
        var root = (await db.Places.GetPlaceAsync("program", 1, "owner", 1, default))!;
        var rules = new PlacementActivityRules("unrelated", null, false, false, true, false);
        var context = new PositionAlgorithmStrategyContext("program", 1, 3, root, 0, true, 1, [], 2, 10, rules);
        foreach (var algorithm in Algorithms(db.Places))
            Assert.Equal("inviter", (await algorithm.FindNextAsync(context, default))?.ProfileAddr);

        // A matching active invite in another program must never open this parent's spillover.
        await db.InsertPlace(6, "other-program", 0, "owner", null, true);
        await db.Sql("UPDATE places SET is_active=false WHERE id=2");
        foreach (var algorithm in Algorithms(db.Places))
            Assert.Null(await algorithm.FindNextAsync(context, default));
    }

    [DockerPostgresFact]
    public async Task Placement_permissions_never_bypass_width_terminal_clones_or_position_locks()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "inviter", null, false);
        var root = (await db.Places.GetPlaceAsync("program", 1, "inviter", 1, default))!;
        var rules = new PlacementActivityRules("member", "inviter", true, true, false, true);
        var context = new PositionAlgorithmStrategyContext("program", 1, 3, root, 0, true, 1, [], 2, 10, rules);
        foreach (var algorithm in Algorithms(db.Places))
            Assert.Null(await algorithm.FindNextAsync(context with { RootProfileLockMps = [root.Mp + "00000001"] }, default));
        await db.Sql("UPDATE places SET kind=2 WHERE id=4");
        foreach (var algorithm in Algorithms(db.Places)) Assert.Null(await algorithm.FindNextAsync(context, default));
        await db.Sql("UPDATE places SET kind=0, filling=3 WHERE id=4");
        foreach (var algorithm in Algorithms(db.Places)) Assert.Null(await algorithm.FindNextAsync(context, default));
    }

    [DockerPostgresFact]
    public async Task Placement_manual_checks_are_opt_in_and_use_the_candidates_invite()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "inviter", null, false);
        var resolver = new RequestedPositionResolver(db.Places);
        var position = new RequestedPosition(1, "inviter", 1, 1);
        var rules = new PlacementActivityRules("unrelated", null, false, false, true, false);
        Assert.True((await resolver.ResolveAsync("program", 1, 3, 0, position, null, [], default, rules)).IsSuccess);
        rules = rules with { CheckManualPlacement = true, AllowInactiveSpillover = true };
        Assert.False((await resolver.ResolveAsync("program", 1, 3, 0, position, null, [], default, rules)).IsSuccess);
        await db.Sql("UPDATE places SET is_active=true WHERE id=2");
        Assert.True((await resolver.ResolveAsync("program", 1, 3, 0, position, null, [], default, rules)).IsSuccess);
        var activeProfiles = await db.Places.GetActiveInviteProfilesAsync("program", ["inviter", "member"], default);
        Assert.Contains("inviter", activeProfiles);
        Assert.DoesNotContain("member", activeProfiles);
        Assert.False((await resolver.ResolveAsync("program", 1, 3, 0, position, null, ["00000004"], default, rules)).IsSuccess);
    }

    [DockerPostgresFact]
    public async Task Placement_service_uses_the_placed_profile_for_purchases_clones_and_reinvests()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "inviter", null, false);
        await db.Sql("""
            UPDATE structures SET activity='{"type":"marketing","when_inactive":{"allow_own_children":true}}',
              pos_algo='{"v":1,"root":"owner","relation":"relative","groups":[{"id":0,"algo":"classic","weight":1}]}'
            WHERE structure_number=1
            """);
        var service = new NextPosService(new NextPositionQueries(db.Structures, db.Places),
            new PositionAlgorithmConfigurationParser(), new PositionGroupSelector(),
            new PositionRootResolver([new OwnerRootPlaceStrategy(db.Places)]),
            new PositionAlgorithmResolver(Algorithms(db.Places)), new NoLocks(), db.Places);
        foreach (var operation in new[] { PositionOperation.BuyPlace, PositionOperation.BuyFirstPlace,
            PositionOperation.CreateClone, PositionOperation.CreateReinvest })
        {
            Assert.Equal("inviter", (await service.GetNextPosAsync("program", 1, "member", operation, default))?.ProfileAddr);
            Assert.Null(await service.GetNextPosAsync("program", 1, "unrelated", operation, default));
        }
    }

    private sealed class NoLocks : IPositionLockQueries
    {
        public Task<string[]> GetAllLockMpsAsync(string marketingAddr, byte structureNumber,
            string? profileAddr, CancellationToken cancellationToken) => Task.FromResult(Array.Empty<string>());
    }
}
