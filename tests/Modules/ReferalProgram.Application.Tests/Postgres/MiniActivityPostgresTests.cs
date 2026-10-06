using System.Text.Json;
using Dapper;
using IntegrationRequests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ReferalProgram.Infrastructure.Persistence;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    private static readonly DateTime MiniTime = new(2026, 3, 31, 12, 0, 0, DateTimeKind.Utc);
    private static long MiniCutoff => new DateTimeOffset(MiniTime.AddMonths(-1)).ToUnixTimeSeconds();

    [DockerPostgresFact]
    public async Task Expiration_is_scoped_atomic_repeatable_and_has_no_volume_effects()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        for (var id = 4; id <= 12; id++)
            await db.InsertPlace(id, id == 10 ? "other-program" : "program", id == 11 ? (byte)1 : (byte)0, "p" + id, 1, id != 4);
        await db.Sql("UPDATE places SET activated_at=@old, is_active=true WHERE id IN (1,2,3)", new { old = MiniCutoff - 1 });
        await db.Sql("UPDATE places SET activated_at=@old WHERE id BETWEEN 4 AND 12", new { old = MiniCutoff - 1 });
        await db.Sql("UPDATE places SET activated_at=@boundary WHERE id=5; UPDATE places SET activated_at=@recent WHERE id IN (6,12)", new { boundary = MiniCutoff, recent = MiniCutoff + 1 });
        await db.Sql("UPDATE places SET activated_at=NULL WHERE id=7; UPDATE places SET place_number=2 WHERE id=8; UPDATE places SET profile_addr=NULL WHERE id=9");
        await db.Sql("INSERT INTO profile_volumes(marketing_addr,structure_number,profile_addr,personal_volume,referral_volume,group_volume) VALUES ('program',0,'inviter',7,11,13),('program',1,'inviter',17,19,23)");
        await db.ExpireMini();
        await db.ExpireMini();
        foreach (var id in new[] { 2, 3, 4 })
        {
            var expired = (await db.Places.GetPlaceAsync(id, default))!;
            Assert.False(expired.IsActive);
            Assert.Null(expired.ActivatedAt);
        }
        foreach (var id in new[] { 1, 5, 6, 7, 8, 9, 10, 11, 12 })
            Assert.True((await db.Places.GetPlaceAsync(id, default))!.IsActive);
        Assert.Equal(MiniCutoff, (await db.Places.GetPlaceAsync(5, default))!.ActivatedAt);
        Assert.Equal(MiniCutoff - 1, (await db.Places.GetPlaceAsync(1, default))!.ActivatedAt);
        Assert.Equal(90L, await db.Scalar("SELECT sum(personal_volume+referral_volume+group_volume) FROM profile_volumes"));
        Assert.Equal(0L, await db.Count("marketing_tasks"));
        // A retry of the old occurrence cannot erase a later activation.
        await db.Sql("UPDATE places SET is_active=true,activated_at=@recent WHERE id=2", new { recent = MiniCutoff + 1 });
        await db.ExpireMini();
        Assert.True((await db.Places.GetPlaceAsync(2, default))!.IsActive);
        Assert.Equal(MiniCutoff + 1, (await db.Places.GetPlaceAsync(2, default))!.ActivatedAt);
    }

    [DockerPostgresFact]
    public async Task Mini_scripts_execute_and_schedule_only_daily_invite_expiration()
    {
        await using var db = await Database.Create();
        // Match production migration defaults not supplied by EF EnsureCreated.
        await db.Sql("ALTER TABLE referal_program ALTER COLUMN is_task_processing_enabled SET DEFAULT true; ALTER TABLE places ALTER COLUMN matrix_filling SET DEFAULT 1");
        var setup = ReadMiniScript("ReferalProgram/Database/Scripts/setup_mini_program.sql")
            .Replace("v_database_username text := '';", "v_database_username text := 'postgres';")
            .Replace("v_marketing_addr text := '';", "v_marketing_addr text := 'program';")
            .Replace("v_owner_profile_addr text := '';", "v_owner_profile_addr text := 'owner';")
            .Replace("v_owner_profile_login text := '';", "v_owner_profile_login text := 'owner';");
        await db.Sql(setup);
        Assert.Equal(18L, await db.Count("structures"));
        var settings = ((await db.Structures.GetStructureAsync("program", 0, default))!.Activity)!.Value;
        var invite = (InviteActivitySettings)ActivitySettings.Parse(settings, 0);
        Assert.True(invite.RequireMarketingPlaceToInvite);
        Assert.False(invite.WhenInactive.AllowInvitingWithoutPlaces);
        Assert.True(invite.WhenInactive.AllowInvitingWithPlaces);
        await db.Sql(ReadMiniScript("ReferalProgram/Database/Scripts/set_mini_activity.sql").Replace("v_marketing_addr text := '';", "v_marketing_addr text := 'program';"));
        await db.Sql(ReadMiniScript("ReferalProgram/Database/Scripts/set_mini_activation_groups.sql").Replace("v_marketing_addr text := '';", "v_marketing_addr text := 'program';"));
        for (byte number = 1; number <= 17; number++)
        {
            var activity = (await db.Structures.GetStructureAsync("program", number, default))!.Activity;
            if (number <= 3)
            {
                var marketing = (MarketingActivitySettings)ActivitySettings.Parse(activity!.Value, number);
                Assert.Equal("group_root", marketing.ActivitySource);
                Assert.False(marketing.WhenInactive.AllowSpilloverChildren);
                Assert.False(marketing.WhenInactive.CheckManualPlacement);
                Assert.False(marketing.PreserveStatusOnActivation);
            }
            else Assert.Null(activity);
        }
        await db.Sql(ReadMiniScript("ScheduledTasks/Database/Scripts/001_create_tasks.sql"));
        var task = ReadMiniScript("ScheduledTasks/Database/Scripts/add_mini_activity_expiration_task.sql")
            .Replace("v_marketing_address text := '';", "v_marketing_address text := 'program';")
            .Replace("v_first_execution_at_utc timestamptz := NULL;", "v_first_execution_at_utc timestamptz := '2026-04-01 00:00:00+00';");
        await db.Sql(task);
        Assert.Equal(1L, await db.Count("tasks"));
        Assert.Equal(1L, await db.Scalar("SELECT count(*) FROM tasks WHERE schedule='{" + "\"type\":\"interval\",\"unit\":\"days\",\"value\":1}'::jsonb AND commands->0->'arguments'='{\"structureNumber\":1,\"period\":{\"unit\":\"months\",\"value\":1}}'::jsonb AND jsonb_array_length(commands)=1"));
        await Assert.ThrowsAsync<PostgresException>(() => db.Sql(task));
        Assert.Equal(1L, await db.Count("tasks"));
    }

    [DockerPostgresFact]
    public async Task Mini_activation_receipt_and_clone_belong_to_activated_profile_not_parent()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.Sql("""
            INSERT INTO referal_program(marketing_addr,is_task_processing_enabled)
            VALUES ('program',true) ON CONFLICT DO NOTHING;
            INSERT INTO structures SELECT marketing_addr,2,max_places_per_profile,width,height,display_height,prev_required,pos_algo,activity,"group" FROM structures WHERE structure_number=1;
            INSERT INTO structures SELECT marketing_addr,3,max_places_per_profile,width,height,display_height,prev_required,pos_algo,activity,"group" FROM structures WHERE structure_number=1;
            UPDATE structures SET "group"='Mini 10', height=1, max_places_per_profile=0,
                pos_algo='{"v":1,"root":"owner","relation":"relative","groups":[{"id":0,"algo":"trimmed_classic","weight":1,"cut_factor":2}]}'
                WHERE structure_number BETWEEN 1 AND 3;
            """);
        await db.Sql(ReadMiniScript("ReferalProgram/Database/Scripts/set_mini_activity.sql")
            .Replace("v_marketing_addr text := '';", "v_marketing_addr text := 'program';"));
        await db.InsertPlace(4, "program", 1, "owner", null, true);
        await db.InsertPlace(5, "program", 1, "member", 4, false);

        var activation = await db.Activate(1, 1);
        Assert.True(activation.IsSuccess, string.Join("; ", activation.Errors));
        Assert.Equal("member", activation.Value.Source.ProfileAddr);
        Assert.Equal<uint>(1, activation.Value.Source.PlaceNumber);
        Assert.Equal<uint>(0, activation.Value.Code);
        Assert.Equal(5L, await db.Scalar("SELECT response_source_place_id FROM marketing_tasks WHERE task_key=1"));
        Assert.Equal(0L, await db.Scalar("SELECT response_code FROM marketing_tasks WHERE task_key=1"));

        var clone = await db.PlaceCommand(PositionOperation.CreateClone, source: activation.Value.Source, taskKey: 2);
        Assert.True(clone.IsSuccess, string.Join("; ", clone.Errors));
        var created = await db.Places.GetPlaceAsync("program", 1, "member", 2, default);
        Assert.NotNull(created);
        Assert.Equal(PlaceKinds.Clone, created.Kind);
        Assert.True(created.IsActive);
        Assert.NotNull(created.ActivatedAt);
        Assert.Null(await db.Places.GetPlaceAsync("program", 1, "owner", 2, default));
        // Clone responses still use the ordinary height-based reward source.
        Assert.Equal("owner", clone.Value.Source.ProfileAddr);
        Assert.Equal(2L, await db.Count("marketing_tasks"));
        Assert.Equal(2L, await db.Scalar("SELECT personal_volume FROM profile_volumes WHERE profile_addr='member' AND structure_number=1"));
        Assert.Equal(2L, await db.Scalar("SELECT referral_volume FROM profile_volumes WHERE profile_addr='inviter' AND structure_number=1"));
    }

    [DockerPostgresFact]
    public async Task Mini_invitations_require_marketing_places_regardless_of_invite_status()
    {
        await using var db = await Database.Create();
        foreach (var active in new[] { false, true })
        foreach (var hasPlaces in new[] { false, true })
        {
            await db.Reset(MiniInviteJson());
            await db.Sql("UPDATE places SET is_active=@active WHERE id=2", new { active });
            await db.InsertPlace(4, "other-program", 1, "inviter", null, true);
            if (hasPlaces) await db.InsertPlace(5, "program", 4, "inviter", null, false);
            var result = await db.Choose();
            Assert.Equal(hasPlaces, result.IsSuccess);
            Assert.Equal(hasPlaces ? 1L : 0L, await db.Count("marketing_tasks"));
        }
    }

    [DockerPostgresFact]
    public async Task Expired_mini_group_root_only_loses_automatic_spillovers_in_first_three_structures()
    {
        await using var db = await Database.Create();
        await db.Reset(MiniInviteJson());
        await db.Sql("UPDATE places SET is_active=true,activated_at=@old WHERE id=2", new { old = MiniCutoff - 1 });
        for (byte number = 1; number <= 4; number++)
        {
            if (number > 1) await db.Sql("INSERT INTO structures SELECT marketing_addr,@number,max_places_per_profile,width,height,display_height,prev_required,pos_algo,activity,\"group\" FROM structures WHERE structure_number=1", new { number = (short)number });
            await db.InsertPlace(30 + number, "program", number, "owner", null, true);
            await db.InsertPlace(10 + number, "program", number, "inviter", 30 + number, true);
            await db.InsertPlace(20 + number, "program", number, "inviter", 10 + number, true);
            await db.Sql("UPDATE places SET place_number=2 WHERE id=@id", new { id = 20 + number });
        }
        var marketingJson = System.Text.RegularExpressions.Regex.Match(ReadMiniScript("ReferalProgram/Database/Scripts/set_mini_activity.sql"), "ELSE '(.*?)'::jsonb").Groups[1].Value;
        await db.Sql("UPDATE structures SET activity=CAST(@marketingJson AS jsonb),\"group\"='Mini 10' WHERE structure_number BETWEEN 1 AND 3", new { marketingJson });
        await db.Sql("UPDATE places SET activated_at=@old WHERE id=11", new { old = MiniCutoff - 1 });
        foreach (var expired in new[] { false, true })
        {
            if (expired) await db.ExpireMini(1);
            for (byte number = 1; number <= 4; number++)
            {
                var settings = number <= 3 ? (MarketingActivitySettings)ActivitySettings.Parse((await db.Structures.GetStructureAsync("program", number, default))!.Activity!.Value, number) : null;
                foreach (var relation in new[] { "own", "invited", "spillover" })
                {
                    var rules = settings is null ? null : new PlacementActivityRules(relation == "own" ? "inviter" : "child", relation == "invited" ? "inviter" : null,
                        settings.WhenInactive.AllowOwnChildren, settings.WhenInactive.AllowSpilloverChildren, false,
                        await ActivitySourceResolver.ResolveAsync((await db.Structures.GetStructureAsync("program", number, default))!, db.Places, default));
                    var root = (await db.Places.GetPlaceAsync(10 + number, default))!;
                    var candidates = await db.Places.GetOpenPlacesByMpPrefixAsync("program", number, root.Mp, 2, 1, 50, default, rules);
                    Assert.Equal(expired && number <= 3 && relation == "spillover" ? 0 : 2, candidates.Count);
                    Assert.Equal(!(expired && number == 1), root.IsActive);
                    // Manual placement remains allowed even when the automatic spillover gate rejects it.
                    if (rules is not null) Assert.True(rules.Allows(root, inviteActive: !expired, manual: true));
                }
            }
        }
        Assert.True((await db.Places.GetPlaceAsync(2, default))!.IsActive);
        Assert.True((await db.Choose()).IsSuccess);
    }

    [DockerPostgresFact]
    public async Task Expiration_rechecks_date_after_a_concurrent_renewal_commits()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.Sql("UPDATE places SET is_active=true,activated_at=@old WHERE id=2", new { old = MiniCutoff - 1 });
        await db.CheckConcurrentRenewal();
        var invite = (await db.Places.GetPlaceAsync(2, default))!;
        Assert.True(invite.IsActive);
        Assert.Equal(MiniCutoff + 1, invite.ActivatedAt);
    }

    private static string MiniInviteJson()
    {
        var sql = ReadMiniScript("ReferalProgram/Database/Scripts/set_mini_activity.sql");
        return System.Text.RegularExpressions.Regex.Match(sql, "THEN '(.*?)'::jsonb").Groups[1].Value;
    }
    private static string ReadMiniScript(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src/Modules", relative))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, "src/Modules", relative));
    }
    private sealed partial class Database
    {
        public async Task CheckConcurrentRenewal()
        {
            await using var renewal = await data.OpenConnectionAsync();
            await using var transaction = await renewal.BeginTransactionAsync();
            await renewal.ExecuteAsync("UPDATE places SET activated_at=@recent WHERE id=2", new { recent = MiniCutoff + 1 }, transaction);
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            await context.Database.OpenConnectionAsync();
            var pid = ((NpgsqlConnection)context.Database.GetDbConnection()).ProcessID;
            var update = scope.ServiceProvider.GetRequiredService<IPlaceRepository>()
                .ExpireFirstPlacesAsync("program", 0, MiniCutoff, default);
            var waited = false;
            for (var attempt = 0; attempt < 200; attempt++)
            {
                if (await Scalar("SELECT count(*) FROM pg_stat_activity WHERE pid=@pid AND wait_event_type='Lock'", new { pid }) == 1)
                {
                    waited = true;
                    break;
                }
                await Task.Delay(10);
            }
            await transaction.CommitAsync();
            Assert.Equal(0, await update);
            Assert.True(waited, "The expiration UPDATE must wait for the renewing transaction before rechecking the date.");
        }

        public async Task ExpireMini(byte number = 0)
        {
            await using var scope = provider.CreateAsyncScope();
            await new ExpiredFirstPlaceDeactivationService(scope.ServiceProvider.GetRequiredService<IPlaceRepository>(), Structures)
                .ExecuteAsync(new("program", number, new("months", 1), Guid.NewGuid(), MiniTime), default);
        }
    }
}
