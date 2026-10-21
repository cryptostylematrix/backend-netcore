using Npgsql;
using ProgramPlaceOwnerChanger;
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
            Assert.Equal("parent", await Scalar(connection, "SELECT parent_profile_addr FROM places WHERE id=2"));
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
            await Assert.ThrowsAsync<OwnerChangeException>(() => service.RunAsync(request with { NewAddress = "missing" }, false, default));
        });
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
                    (1,'m',0,'new','newlogin',1,'newlogin1','parent','parentlogin',null,null,1),
                    (2,'m',1,'old','oldlogin',1,'oldlogin1','oldparent','oldparent',null,null,1),
                    (3,'m',1,'old','oldlogin',2,'oldlogin2','oldparent','oldparent',2,1,null),
                    (4,'m',1,'other','other',1,'other1','old','oldlogin',3,2,1),
                    (5,'m',1,'new','newlogin',5,'newlogin5','parent','parentlogin',null,null,1),
                    (6,'m',2,'old','oldlogin',9,'oldlogin9','oldparent','oldparent',null,null,1);
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
