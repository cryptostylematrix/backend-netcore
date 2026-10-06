using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Dto;
using ReferalProgram.Application.Services.RootStrategies;
using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Activity_source_replaces_own_status_for_every_algorithm_and_manual_policy()
    {
        await using var db = await Database.Create();
        await db.SeedActivitySources();
        foreach (var source in new[] { "place", "invite", "group_root" })
        foreach (var ownActive in new[] { false, true })
        foreach (var sourceActive in new[] { false, true })
        foreach (var allowOwn in new[] { false, true })
        foreach (var allowSpill in new[] { false, true })
        {
            await db.SourceSettings(source, allowOwn, allowSpill);
            await db.Sql("UPDATE places SET is_active=@ownActive WHERE id=6; UPDATE places SET is_active=@sourceActive WHERE id IN (2,5)", new { ownActive, sourceActive });
            var structure = (await db.Structures.GetStructureAsync("program", 2, default))!;
            var sourceNumber = await ActivitySourceResolver.ResolveAsync(structure, db.Places, default);
            var root = (await db.Places.GetPlaceAsync(6, default))!;
            foreach (var own in new[] { false, true })
            {
                var active = source == "place" ? ownActive : sourceActive;
                var expected = active || (own ? allowOwn : allowSpill);
                var rules = new PlacementActivityRules(own ? "inviter" : "other", null, allowOwn, allowSpill, true, sourceNumber);
                var context = new PositionAlgorithmStrategyContext("program", 2, 3, root, 0, true, 1, [], 2, 10, rules);
                foreach (var algorithm in Algorithms(db.Places))
                    Assert.True((await algorithm.FindNextAsync(context, default) is not null) == expected,
                        $"{algorithm.Name}, {source}, ownActive={ownActive}, sourceActive={sourceActive}, own={own}");
                var manual = await new RequestedPositionResolver(db.Places).ResolveAsync("program", 2, 3, 0,
                    new(2, "inviter", 1, 1), null, [], default, rules);
                Assert.Equal(expected, manual.Position is not null);
            }
        }
    }

    [DockerPostgresFact]
    public async Task Group_root_is_program_scoped_minimum_structure_not_first_available_place()
    {
        await using var db = await Database.Create();
        await db.SeedActivitySources();
        await db.Sql("INSERT INTO structures SELECT 'other-program',0,max_places_per_profile,width,height,display_height,prev_required,pos_algo,activity,\"group\" FROM structures WHERE structure_number=1");
        Assert.Equal((byte)1, await db.Places.GetGroupRootStructureAsync("program", 2, default));
        await db.SourceSettings("group_root", false, false);
        var structure = (await db.Structures.GetStructureAsync("program", 2, default))!;
        var source = await ActivitySourceResolver.ResolveAsync(structure, db.Places, default);
        await db.Sql("UPDATE places SET profile_addr='someone-else' WHERE id=5; UPDATE places SET is_active=true WHERE id=6");
        var place = (await db.Places.GetPlaceAsync(6, default))!;
        Assert.False(await ActivitySourceResolver.IsActiveAsync(place, source, db.Places, new Dictionary<string, bool>(), default));
        await db.InsertPlace(9, "other-program", 1, "inviter", null, true);
        Assert.Empty(await db.Places.GetActiveSourceProfilesAsync("program", 1, ["inviter"], default));
        Assert.Null(await db.Places.GetFirstActiveUnfilledPlaceAsync("program", 2, place.Mp, 3, true, 1, [], default,
            new("child", null, false, false, false, source)));
        await db.Sql("UPDATE structures SET \"group\"=NULL WHERE structure_number=2 AND marketing_addr='program'");
        var ungrouped = (await db.Structures.GetStructureAsync("program", 2, default))!;
        await Assert.ThrowsAsync<JsonException>(() => ActivitySourceResolver.ResolveAsync(ungrouped, db.Places, default));
    }

    [DockerPostgresFact]
    public async Task Recipients_use_source_status_and_separate_bonus_clone_permissions()
    {
        await using var db = await Database.Create();
        await db.SeedActivitySources();
        await db.InsertPlace(7, "program", 2, "member", 6, true);
        foreach (var source in new[] { "invite", "group_root" })
        foreach (var active in new[] { false, true })
        foreach (var allowBonus in new[] { false, true })
        foreach (var allowClone in new[] { false, true })
        {
            await db.SourceSettings(source, false, false, allowBonus, allowClone);
            await db.Sql("UPDATE places SET is_active=@active WHERE id IN (2,5); UPDATE places SET is_active=NOT @active WHERE id=6", new { active });
            var resolver = new RelativePlaceResolver(db.Places, db.Structures);
            foreach (var purpose in new[] { RecipientPurpose.Bonus, RecipientPurpose.Clone })
            {
                var allowed = active || (purpose == RecipientPurpose.Bonus ? allowBonus : allowClone);
                var result = await resolver.ResolveAsync("program", 2, "inviter", 1, 0, default, purpose);
                Assert.Equal(allowed, result is not null);
                if (result is not null) Assert.Equal(6, result.RelativePlace.Id);
            }
        }
    }

    [DockerPostgresFact]
    public async Task Compression_uses_group_source_without_mutating_own_status()
    {
        await using var db = await Database.Create();
        await db.Sql("CREATE TABLE structure_ranks(marketing_addr text,structure_number smallint,name text,required_active_referral_places bigint)");
        foreach (var active in new[] { false, true })
        foreach (var keep in new[] { false, true })
        {
            await db.SeedActivitySources();
            await db.InsertPlace(7, "program", 2, "member", 6, true);
            await db.InsertPlace(8, "program", 1, "member", 5, false);
            await db.SourceSettings("group_root", false, false, keep: keep);
            await db.Sql("UPDATE places SET is_active=@active WHERE id=5; UPDATE places SET is_active=NOT @active WHERE id=6", new { active });
            var error = await db.Compress(2);
            Assert.Equal(active || keep, error is null);
            Assert.Equal(!active, (await db.Places.GetPlaceAsync(6, default))!.IsActive);
            if (error is null) Assert.Equal(keep, await db.Places.GetPlaceAsync(7, default) is not null);
        }
    }

    [DockerPostgresFact]
    public async Task Next_position_resolves_group_source_once_and_restores_spillover_after_reactivation()
    {
        await using var db = await Database.Create();
        await db.SeedActivitySources();
        await db.SourceSettings("group_root", false, false);
        await db.Sql("UPDATE places SET is_active=true WHERE id=5; UPDATE places SET is_active=false WHERE id=6");
        var selection = await db.SourceSelection("other");
        Assert.NotNull(selection);
        Assert.Equal((byte)1, selection.Context.Activity!.ActivityStructureNumber);
        Assert.NotNull(await Algorithms(db.Places).Single(a => a.Name == "classic").FindNextAsync(selection.Context, default));
        await db.Sql("UPDATE places SET is_active=false WHERE id=5");
        Assert.Null(await Algorithms(db.Places).Single(a => a.Name == "classic").FindNextAsync(selection.Context, default));
        await db.Sql("UPDATE places SET is_active=true WHERE id=5");
        Assert.NotNull(await Algorithms(db.Places).Single(a => a.Name == "classic").FindNextAsync(selection.Context, default));
        Assert.False((await db.Places.GetPlaceAsync(6, default))!.IsActive);
    }

    private sealed partial class Database
    {
        public Task<PositionSelection?> SourceSelection(string profile) =>
            new NextPosService(new NextPositionQueries(Structures, Places),
                new PositionAlgorithmConfigurationParser(), new PositionGroupSelector(),
                new PositionRootResolver([new OwnerRootPlaceStrategy(Places)]),
                new PositionAlgorithmResolver(Algorithms(Places)), new LockQueries(data), Places)
                .ResolveSelectionAsync("program", 2, profile, PositionOperation.BuyPlace, default);

        public async Task SeedActivitySources()
        {
            await Reset(null);
            await Sql("INSERT INTO structures SELECT marketing_addr,2,max_places_per_profile,width,height,display_height,prev_required,pos_algo,activity,' G ' FROM structures WHERE structure_number=1; UPDATE structures SET \"group\"='G' WHERE structure_number=1");
            await InsertPlace(4, "program", 1, "owner", null, true);
            await InsertPlace(5, "program", 1, "inviter", 4, false);
            await InsertPlace(6, "program", 2, "inviter", null, false);
        }
        public Task SourceSettings(string source, bool own, bool spill, bool bonus = false, bool clone = false, bool keep = false) => Sql("""
            UPDATE structures SET activity=CAST(@json AS jsonb),
                pos_algo='{"v":1,"root":"owner","relation":"absolute","groups":[{"id":0,"algo":"classic","weight":1}]}'
            WHERE marketing_addr='program' AND structure_number=2
            """, new { json = JsonSerializer.Serialize(new { type = "marketing", activity_source = source,
                when_inactive = new { allow_own_children = own, allow_spillover_children = spill,
                    allow_as_bonus_recipient = bonus, allow_as_clone_recipient = clone, keep_on_compression = keep } }) });
    }
}
