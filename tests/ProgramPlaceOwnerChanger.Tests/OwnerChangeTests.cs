using Npgsql;
using ProgramPlaceOwnerChanger;
using ProgramPlaceParentRepairer;
using Xunit;

namespace ProgramPlaceOwnerChanger.Tests;

public sealed class OwnerChangeTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("1", 1)]
    [InlineData("255", 255)]
    public void Parses_structure(string? input, int? expected) =>
        Assert.Equal(expected, (int?)OwnerChangeRequest.ParseStructure(input));

    [Theory]
    [InlineData("0")]
    [InlineData("256")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Rejects_invalid_structure(string input) =>
        Assert.Throws<ArgumentException>(() => OwnerChangeRequest.ParseStructure(input));

    [PostgresFact]
    public async Task Transfer_renumbers_locks_and_children_rebuilds_volumes_and_respects_scope()
    {
        await WithDatabase(async (connection, service) =>
        {
            var request = new OwnerChangeRequest("m", 1, "old", "new", "newlogin");
            var preview = await service.RunAsync(request, false, default);
            Assert.Equal(2, preview.Places);
            Assert.Equal(3, preview.Locks);
            Assert.Equal(2L, await Scalar(connection, "SELECT count(*) FROM places WHERE structure_number=1 AND profile_addr='old'"));
            await service.RunAsync(request, true, default, preview);
            Assert.Equal("6,7", await Scalar(connection, "SELECT string_agg(place_number::text, ',' ORDER BY place_number) FROM places WHERE structure_number=1 AND profile_addr='new' AND id IN (2,3)"));
            Assert.Equal("newlogin6", await Scalar(connection, "SELECT \"index\" FROM places WHERE id=2"));
            Assert.Equal("physical", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=2"));
            Assert.Equal("physicallogin", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=2"));
            Assert.Equal(7, await Scalar(connection, "SELECT parent_id FROM places WHERE id=2"));
            Assert.Equal(1L, await Scalar(connection, "SELECT parent_place_number FROM places WHERE id=2"));
            Assert.Equal("new:newlogin:6", await Scalar(connection, "SELECT parent_profile_addr || ':' || parent_profile_login || ':' || parent_place_number FROM places WHERE id=3"));
            Assert.Equal("new:newlogin:7", await Scalar(connection, "SELECT parent_profile_addr || ':' || parent_profile_login || ':' || parent_place_number FROM places WHERE id=4"));
            Assert.Empty((await RepairService(connection).RunAsync("m", 1, false, default)).Mismatches);
            Assert.Equal(7L, await Scalar(connection, "SELECT parent_place_number FROM places WHERE id=4"));
            Assert.Equal(6L, await Scalar(connection, "SELECT place_number FROM locks WHERE id=1"));
            Assert.Equal("new:new", await Scalar(connection, "SELECT profile_addr || ':' || place_profile_addr FROM locks WHERE id=1"));
            Assert.Equal("other:new", await Scalar(connection, "SELECT profile_addr || ':' || place_profile_addr FROM locks WHERE id=2"));
            Assert.Equal("new:other", await Scalar(connection, "SELECT profile_addr || ':' || place_profile_addr FROM locks WHERE id=3"));
            Assert.Equal("old", await Scalar(connection, "SELECT profile_addr FROM places WHERE id=6"));
            Assert.Equal(2L, await Scalar(connection, "SELECT personal_volume FROM profile_volumes WHERE structure_number=1 AND profile_addr='new'"));
            Assert.Equal(2L, await Scalar(connection, "SELECT referral_volume FROM profile_volumes WHERE structure_number=1 AND profile_addr='parent'"));
            Assert.Equal(0L, await Scalar(connection, "SELECT personal_volume FROM profile_volumes WHERE structure_number=1 AND profile_addr='old'"));
            Assert.Equal(9L, await Scalar(connection, "SELECT group_volume FROM profile_volumes WHERE structure_number=1 AND profile_addr='old'"));
            Assert.Equal(88L, await Scalar(connection, "SELECT personal_volume FROM profile_volumes WHERE structure_number=2 AND profile_addr='old'"));
        });
    }

    [PostgresFact]
    public async Task Lock_collision_rolls_back_places_and_volumes()
    {
        await WithDatabase(async (connection, service) =>
        {
            await Execute(connection, "INSERT INTO locks VALUES (4,'m',1,'other',8,'other','new',3)");
            var request = new OwnerChangeRequest("m", null, "old", "new", "newlogin");
            var preview = await service.RunAsync(request, false, default);
            await Assert.ThrowsAsync<PostgresException>(() => service.RunAsync(request, true, default, preview));
            Assert.Equal("old", await Scalar(connection, "SELECT profile_addr FROM places WHERE id=2"));
            Assert.Equal(1L, await Scalar(connection, "SELECT place_number FROM places WHERE id=2"));
            Assert.Equal(99L, await Scalar(connection, "SELECT personal_volume FROM profile_volumes WHERE structure_number=1 AND profile_addr='old'"));
        });
    }

    [PostgresFact]
    public async Task All_structures_and_missing_invite()
    {
        await WithDatabase(async (connection, service) =>
        {
            var request = new OwnerChangeRequest("m", null, "old", "new", "newlogin");
            var preview = await service.RunAsync(request, false, default);
            await service.RunAsync(request, true, default, preview);
            Assert.Equal("new", await Scalar(connection, "SELECT profile_addr FROM places WHERE id=6"));
            Assert.Equal(1L, await Scalar(connection, "SELECT place_number FROM places WHERE id=6"));
            Assert.Equal("new", await Scalar(connection, "SELECT profile_addr FROM places WHERE id=1"));
            Assert.Equal(DBNull.Value, await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=6"));
            Assert.Equal(DBNull.Value, await Scalar(connection, "SELECT parent_place_number FROM places WHERE id=6"));
            await Assert.ThrowsAsync<OwnerChangeException>(() => service.RunAsync(request with { NewAddress = "missing" }, false, default));
        });
    }

    [PostgresFact]
    public async Task Repair_restores_legacy_transfer_children_roots_and_system_parents_and_is_idempotent()
    {
        await WithDatabase(async (connection, service) =>
        {
            var request = new OwnerChangeRequest("m", 1, "old", "new", "newlogin");
            await service.RunAsync(request, true, default, await service.RunAsync(request, false, default));
            // Reproduce the old transfer bug without changing physical links or owners.
            await Execute(connection, """
                UPDATE places SET parent_profile_addr='parent', parent_profile_login='parentlogin' WHERE id IN (2,3);
                UPDATE places SET parent_profile_addr='old', parent_profile_login='oldlogin' WHERE id=4;
                UPDATE places SET parent_profile_addr='wrong', parent_profile_login='wrong', parent_place_number=99 WHERE id=7;
                INSERT INTO places VALUES
                    (8,'m',1,null,null,1,'system1',null,null,null,null,null),
                    (9,'m',1,'child','child',1,'child1','wrong','wrong',8,99,null),
                    (10,'other-program',1,'other','other',1,'other1','untouched','untouched',null,99,null);
                """);
            var repair = RepairService(connection);
            var preview = await repair.RunAsync("m", 1, false, default);
            Assert.Equal(5, preview.Mismatches.Count);
            Assert.Equal(0, preview.InvalidParents);
            Assert.Equal("parent", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=2"));
            await repair.RunAsync("m", 1, true, default, preview);
            Assert.Empty((await repair.RunAsync("m", 1, false, default)).Mismatches);
            Assert.Equal("physical", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=2"));
            Assert.Equal("new", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=4"));
            Assert.Equal(DBNull.Value, await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=7"));
            Assert.Equal(DBNull.Value, await Scalar(connection, "SELECT parent_place_number FROM places WHERE id=7"));
            Assert.Equal(DBNull.Value, await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=9"));
            Assert.Equal(1L, await Scalar(connection, "SELECT parent_place_number FROM places WHERE id=9"));
            Assert.Equal("untouched", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=10"));
            Assert.Equal("oldparent", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=6"));
            Assert.Equal("parent", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=1"));
            var all = await repair.RunAsync("m", null, false, default);
            Assert.Single(all.Mismatches);
            await repair.RunAsync("m", null, true, default, all);
            Assert.Empty((await repair.RunAsync("m", null, false, default)).Mismatches);
            await repair.RunAsync("m", null, true, default, await repair.RunAsync("m", null, false, default));
        });
    }

    [PostgresFact]
    public async Task Repair_rejects_stale_preview_and_invalid_parents_without_partial_writes()
    {
        await WithDatabase(async (connection, _) =>
        {
            var repair = RepairService(connection);
            var preview = await repair.RunAsync("m", 1, false, default);
            await Execute(connection, "UPDATE places SET parent_profile_login='wrong' WHERE id=4");
            await Assert.ThrowsAsync<ParentRepairException>(() => repair.RunAsync("m", 1, true, default, preview));
            Assert.Equal("wrong", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=4"));
            await Execute(connection, "UPDATE places SET parent_id=999 WHERE id=2; UPDATE places SET parent_id=6 WHERE id=3; UPDATE places SET parent_id=id WHERE id=5");
            preview = await repair.RunAsync("m", 1, false, default);
            Assert.Equal(3, preview.InvalidParents);
            await Assert.ThrowsAsync<ParentRepairException>(() => repair.RunAsync("m", 1, true, default, preview));
            Assert.Equal("wrong", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=4"));
            Assert.Equal(999, await Scalar(connection, "SELECT parent_id FROM places WHERE id=2"));
        });
    }

    [PostgresFact]
    public async Task Transfer_rejects_invalid_physical_parent_without_partial_writes()
    {
        await WithDatabase(async (connection, service) =>
        {
            await Execute(connection, "UPDATE places SET parent_id=6 WHERE id=2");
            var request = new OwnerChangeRequest("m", 1, "old", "new", "newlogin");
            var preview = await service.RunAsync(request, false, default);
            await Assert.ThrowsAsync<OwnerChangeException>(() => service.RunAsync(request, true, default, preview));
            Assert.Equal("old", await Scalar(connection, "SELECT profile_addr FROM places WHERE id=2"));
            Assert.Equal(1L, await Scalar(connection, "SELECT place_number FROM places WHERE id=2"));
        });
    }

    [PostgresFact]
    public async Task Repair_verification_failure_rolls_back_all_updates()
    {
        await WithDatabase(async (connection, _) =>
        {
            await Execute(connection, """
                UPDATE places SET parent_profile_login='before' WHERE id=4;
                CREATE FUNCTION sabotage_parent_repair() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW.id=4 THEN NEW.parent_profile_login := 'blocked'; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER sabotage BEFORE UPDATE ON places FOR EACH ROW EXECUTE FUNCTION sabotage_parent_repair();
                """);
            var repair = RepairService(connection);
            var preview = await repair.RunAsync("m", null, false, default);
            Assert.Equal(2, preview.Mismatches.Count);
            await Assert.ThrowsAsync<ParentRepairException>(() => repair.RunAsync("m", null, true, default, preview));
            Assert.Equal("before", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=4"));
            Assert.Equal("oldparent", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=6"));
        });
    }

    [PostgresFact]
    public async Task Repair_supports_all_registered_programs_structure_zero_and_progress()
    {
        await WithDatabase(async (connection, _) =>
        {
            await Execute(connection, """
                INSERT INTO referal_program VALUES ('second'),('empty');
                INSERT INTO structures VALUES ('second',0),('second',4),('empty',7),('unregistered',0);
                INSERT INTO places VALUES
                    (20,'second',0,'root','root',1,'root1','wrong','wrong',null,99,null),
                    (21,'second',4,'root','root',1,'root1','wrong','wrong',null,99,null),
                    (22,'unregistered',0,'root','root',1,'root1','untouched','untouched',null,99,null);
                UPDATE places SET parent_profile_login='wrong' WHERE id=1;
                """);
            var repair = RepairService(connection);
            var progress = new List<ParentRepairProgress>();
            var referral = await repair.RunAsync(null, 0, false, default, progress: progress.Add);
            Assert.Equal(2, referral.Targets.Count);
            Assert.Equal(3, referral.Checked);
            Assert.Equal(2, referral.Mismatches.Count);
            Assert.Equal(2, progress[^1].Completed);
            Assert.Equal(2, progress[^1].Total);
            Assert.Equal(3, progress[^1].Checked);
            Assert.Equal(new[] { "m", "second" }, referral.Targets.Select(target => target.Marketing));
            await repair.RunAsync(null, 0, true, default, referral);
            Assert.Empty((await repair.RunAsync("", 0, false, default)).Mismatches);
            Assert.Equal("parentlogin", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=1"));
            Assert.Equal(DBNull.Value, await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=20"));
            Assert.Equal("wrong", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=21"));
            var all = await repair.RunAsync("  ", null, false, default);
            Assert.Equal(6, all.Targets.Count);
            Assert.Equal(2, all.Mismatches.Count);
            progress.Clear();
            await repair.RunAsync(null, null, true, default, all, progress.Add);
            Assert.Contains(progress, item => item.Phase == "Repairing" && item.Completed == 6);
            Assert.Contains(progress, item => item.Phase == "Verifying" && item.Completed == 6);
            Assert.Empty((await repair.RunAsync(null, null, false, default)).Mismatches);
            Assert.Equal("untouched", await Scalar(connection, "SELECT parent_profile_login FROM places WHERE id=22"));
            Assert.Empty((await repair.RunAsync("missing", null, false, default)).Targets);
            Assert.Empty((await repair.RunAsync(null, 255, false, default)).Targets);
        });
    }

    [PostgresFact]
    public async Task Repair_rejects_changed_target_list_even_when_added_structure_is_empty()
    {
        await WithDatabase(async (connection, _) =>
        {
            var repair = RepairService(connection);
            var preview = await repair.RunAsync(null, null, false, default);
            await Execute(connection, "INSERT INTO referal_program VALUES ('new'); INSERT INTO structures VALUES ('new',0)");
            await Assert.ThrowsAsync<ParentRepairException>(() => repair.RunAsync(null, null, true, default, preview));
            Assert.Equal("oldparent", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=6"));
        });
    }

    private static ParentRepairService RepairService(NpgsqlConnection connection)
    {
        var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("OWNER_CHANGER_TEST_POSTGRES"))
        {
            Database = connection.Database
        };
        return new ParentRepairService(settings.ConnectionString);
    }

    private static async Task WithDatabase(Func<NpgsqlConnection, OwnerChangeService, Task> test)
    {
        // Only use an explicitly configured disposable server. Each test owns a fresh database.
        var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("OWNER_CHANGER_TEST_POSTGRES"));
        var database = "owner_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();
        await Execute(admin, $"CREATE DATABASE {database}");
        settings.Database = database;
        try
        {
            await using var connection = new NpgsqlConnection(settings.ConnectionString);
            await connection.OpenAsync();
            await Execute(connection, """
                CREATE TABLE referal_program(marketing_addr text PRIMARY KEY);
                INSERT INTO referal_program VALUES ('m');
                CREATE TABLE structures(marketing_addr text, structure_number smallint);
                CREATE TABLE places(id int primary key, marketing_addr text, structure_number smallint,
                    profile_addr text, profile_login text, place_number bigint CHECK(place_number BETWEEN 1 AND 4294967295),
                    "index" text, parent_profile_addr text, parent_profile_login text,
                    parent_id int, parent_place_number bigint, activated_at bigint,
                    UNIQUE(marketing_addr,structure_number,profile_addr,place_number));
                CREATE TABLE locks(id int primary key,marketing_addr text,structure_number smallint,
                    place_profile_addr text,place_number bigint,place_profile_login text,profile_addr text,locked_pos bigint,
                    UNIQUE(marketing_addr,structure_number,place_profile_addr,place_number,profile_addr,locked_pos));
                CREATE TABLE profile_volumes(marketing_addr text,structure_number smallint,profile_addr text,
                    personal_volume bigint,referral_volume bigint,group_volume bigint,
                    PRIMARY KEY(marketing_addr,structure_number,profile_addr));
                INSERT INTO structures VALUES ('m',0),('m',1),('m',2);
                INSERT INTO places VALUES
                    (1,'m',0,'new','newlogin',1,'newlogin1','parent','parentlogin',11,1,1),
                    (2,'m',1,'old','oldlogin',1,'oldlogin1','physical','physicallogin',7,1,1),
                    (3,'m',1,'old','oldlogin',2,'oldlogin2','old','oldlogin',2,1,null),
                    (4,'m',1,'other','other',1,'other1','old','oldlogin',3,2,1),
                    (5,'m',1,'new','newlogin',5,'newlogin5','physical','physicallogin',7,1,1),
                    (6,'m',2,'old','oldlogin',9,'oldlogin9','oldparent','oldparent',null,null,1),
                    (7,'m',1,'physical','physicallogin',1,'physicallogin1',null,null,null,null,null),
                    (11,'m',0,'parent','parentlogin',1,'parentlogin1',null,null,null,null,null);
                INSERT INTO locks VALUES
                    (1,'m',1,'old',1,'oldlogin','old',1),
                    (2,'m',1,'old',2,'oldlogin','other',2),
                    (3,'m',1,'other',8,'other','old',3);
                INSERT INTO profile_volumes VALUES ('m',1,'old',99,99,9),('m',2,'old',88,88,8);
                """);
            await test(connection, new OwnerChangeService(settings.ConnectionString));
        }
        finally { await Execute(admin, $"DROP DATABASE {database} WITH (FORCE)"); }
    }
    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<object?> Scalar(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}

internal sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OWNER_CHANGER_TEST_POSTGRES")))
            Skip = "Set OWNER_CHANGER_TEST_POSTGRES to an explicitly disposable local PostgreSQL server.";
    }
}
