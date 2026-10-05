using Common.Domain;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Invites;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Application.Policies;
using ReferalProgram.Application.Services;
using ReferalProgram.Application.Services.RootStrategies;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgram.Infrastructure.Queries;
using ReferalProgram.Infrastructure.Repositories;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed class DockerPostgresFactAttribute : FactAttribute
{
    public DockerPostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("ACTIVITY_TEST_POSTGRES_PORT") is null)
            Skip = "Opt-in: start the disposable Docker PostgreSQL described in Postgres/README.md.";
    }
}

public sealed class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Legacy_invitation_rules_persist_children_and_receipts_only_for_active_inviters()
    {
        await using var db = await Database.Create();
        foreach (var json in new string?[] { null, "{}", "{\"set_active_on_activation\":true}", "{\"set_active_on_activation\":false}" })
        foreach (var active in new[] { false, true })
        foreach (var hasPlaces in new[] { false, true })
        {
            await db.Reset(json);
            await db.Sql("UPDATE places SET is_active=@active WHERE id=2", new { active });
            if (hasPlaces) await db.InsertPlace(4, "program", 1, "inviter", null, false);
            var result = await db.Choose();
            Assert.True(active == result.IsSuccess, string.Join("; ", result.Errors));
            Assert.Equal(active ? 1L : 0L, await db.Count("marketing_tasks"));
            Assert.Equal(active ? 1L : 0L, await db.Scalar("SELECT count(*) FROM places WHERE profile_addr='new-profile'"));
            Assert.Equal(active ? 2L : 1L, await db.Scalar("SELECT filling FROM places WHERE id=2"));
            if (active)
            {
                var child = await db.Places.GetPlaceAsync("program", 0, "new-profile", 1, default);
                Assert.NotNull(child);
                Assert.False(child.IsActive);
                Assert.Null(child.ActivatedAt);
                Assert.Equal(2, child.ParentId);
            }
        }
    }

    [DockerPostgresFact]
    public async Task Legacy_profile_fallback_skips_inactive_inviter_and_owner_strategy_ignores_invite_status()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "owner", null, false);
        await db.InsertPlace(5, "program", 1, "inviter", 4, false);
        // Constructor compatibility lets this identical test also run on the previous commit.
        var constructor = typeof(ProfileRootPlaceResolver).GetConstructors().Single();
        var resolver = (IProfileRootPlaceResolver)constructor.Invoke(
            constructor.GetParameters().Length == 1 ? [db.Places] : [db.Places, db.Structures]);
        var root = await resolver.ResolveAsync("program", 1, "member", default);
        Assert.Equal("owner", root?.ProfileAddr);
        Assert.Equal(5, (await resolver.ResolveAsync("program", 1, "inviter", default))?.Id);
        var owner = await new OwnerRootPlaceStrategy(db.Places).ResolveAsync(new("program", 1, "member"), default);
        Assert.Equal(4, owner?.Id);
        Assert.False(owner!.IsActive);
    }

    [DockerPostgresFact]
    public async Task Presence_query_counts_inactive_clones_but_not_invites_or_other_programs()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "other-program", 1, "inviter", null, true);
        Assert.False(await db.Places.HasProfilePlacesOutsideInviteStructureAsync("program", "inviter", default));
        await db.InsertPlace(5, "program", 1, "inviter", null, false);
        await db.Sql("UPDATE places SET kind=1, activated_at=NULL WHERE id=5");
        Assert.True(await db.Places.HasProfilePlacesOutsideInviteStructureAsync("program", "inviter", default));
    }

    [DockerPostgresFact]
    public async Task CryptoCash_legacy_activation_commits_volume_receipt_and_period_reset_without_extra_volume()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        // Use the actual legacy CryptoCash activity JSON from the maintained setup script.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src/Modules/ReferalProgram/Database/Scripts/setup_test_cryptocash_program.sql"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var sql = File.ReadAllText(Path.Combine(directory.FullName, "src/Modules/ReferalProgram/Database/Scripts/setup_test_cryptocash_program.sql"));
        var match = System.Text.RegularExpressions.Regex.Match(sql, @"v_structures jsonb :=\s*'(?<json>.*?)'::jsonb", System.Text.RegularExpressions.RegexOptions.Singleline);
        using var config = System.Text.Json.JsonDocument.Parse(match.Groups["json"].Value);
        foreach (var structure in config.RootElement.EnumerateArray().Skip(1))
        {
            var number = structure.GetProperty("structure_number").GetByte();
            await db.Sql("INSERT INTO structures SELECT 'program', @number, 1, 2, 0, 2, false, '{}'::jsonb, CAST(@activity AS jsonb), NULL ON CONFLICT (marketing_addr,structure_number) DO UPDATE SET activity=EXCLUDED.activity, height=0",
                new { number = (short)number, activity = structure.GetProperty("activity").GetRawText() });
            await db.InsertPlace(20 + number, "program", number, "owner", null, true);
            await db.InsertPlace(10 + number, "program", number, "member", 20 + number, false);
            var result = await db.Activate(number, number);
            Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
            var activated = await db.Places.GetPlaceAsync("program", number, "member", 1, default);
            Assert.True(activated!.IsActive);
            Assert.NotNull(activated.ActivatedAt);
            Assert.Equal(1L, await db.Scalar("SELECT personal_volume FROM profile_volumes WHERE profile_addr='member' AND structure_number=@number", new { number = (short)number }));
            Assert.Equal(1L, await db.Scalar("SELECT referral_volume FROM profile_volumes WHERE profile_addr='inviter' AND structure_number=@number", new { number = (short)number }));
            var again = await db.Activate(number, 100 + number);
            Assert.False(again.IsSuccess);
            await db.ResetPeriod(number);
            Assert.True((await db.Places.GetPlaceAsync("program", number, "member", 1, default))!.IsActive);
            await db.ResetPeriod(number);
            var expired = await db.Places.GetPlaceAsync("program", number, "member", 1, default);
            Assert.False(expired!.IsActive);
            Assert.Null(expired.ActivatedAt);
        }
        Assert.Equal(4L, await db.Count("marketing_tasks"));
        Assert.Equal(8L, await db.Scalar("SELECT sum(personal_volume+referral_volume) FROM profile_volumes"));
    }

    [DockerPostgresFact]
    public async Task New_invitation_flags_use_real_program_scoped_place_presence()
    {
        await using var db = await Database.Create();
        foreach (var hasPlaces in new[] { false, true })
        foreach (var without in new[] { false, true })
        foreach (var with in new[] { false, true })
        {
            var activity = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "invite", when_inactive = new
                { allow_inviting_without_places = without, allow_inviting_with_places = with }
            });
            await db.Reset(activity);
            await db.InsertPlace(4, "other-program", 1, "inviter", null, true);
            if (hasPlaces) await db.InsertPlace(5, "program", 1, "inviter", null, false);
            var result = await db.Choose();
            var expected = hasPlaces ? with : without;
            Assert.True(expected == result.IsSuccess, string.Join("; ", result.Errors));
            Assert.Equal(expected ? 1L : 0L, await db.Count("marketing_tasks"));
            Assert.Equal(expected ? 1L : 0L, await db.Scalar("SELECT count(*) FROM places WHERE profile_addr='new-profile'"));
        }
    }

    [DockerPostgresFact]
    public async Task New_fallback_flag_changes_only_profile_root_selection()
    {
        await using var db = await Database.Create();
        foreach (var allow in new[] { false, true })
        {
            await db.Reset(System.Text.Json.JsonSerializer.Serialize(new
            { type = "invite", when_inactive = new { allow_as_fallback_root = allow } }));
            await db.InsertPlace(4, "program", 1, "owner", null, false);
            await db.InsertPlace(5, "program", 1, "inviter", 4, false);
            var constructor = typeof(ProfileRootPlaceResolver).GetConstructors().Single();
            var resolver = (IProfileRootPlaceResolver)constructor.Invoke(
                constructor.GetParameters().Length == 1 ? [db.Places] : [db.Places, db.Structures]);
            Assert.Equal(allow ? "inviter" : "owner",
                (await resolver.ResolveAsync("program", 1, "member", default))?.ProfileAddr);
            Assert.Equal(4, (await new OwnerRootPlaceStrategy(db.Places)
                .ResolveAsync(new("program", 1, "member"), default))?.Id);
        }
    }

    private sealed class Database : IAsyncDisposable
    {
        private readonly string name = "activity_test_" + Guid.NewGuid().ToString("N");
        private readonly NpgsqlDataSource admin;
        private NpgsqlDataSource data = null!;
        private ServiceProvider provider = null!;
        public PlaceQueries Places => new(data);
        public StructureQueries Structures => new(data);
        private Database(int port) => admin = NpgsqlDataSource.Create($"Host=127.0.0.1;Port={port};Database=activity_test;Username=postgres;Password=activity-test-only;Pooling=false");
        public static async Task<Database> Create()
        {
            var port = int.Parse(Environment.GetEnvironmentVariable("ACTIVITY_TEST_POSTGRES_PORT")!);
            var db = new Database(port);
            try
            {
                await using (var command = db.admin.CreateCommand($"CREATE DATABASE {db.name}")) await command.ExecuteNonQueryAsync();
                var cs = new NpgsqlConnectionStringBuilder(db.admin.ConnectionString) { Database = db.name, Password = "activity-test-only" }.ConnectionString;
                db.data = NpgsqlDataSource.Create(cs);
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddDbContext<DataContext>(o => o.UseNpgsql(cs));
                services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
                services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(ChooseInviterCommand).Assembly));
                services.AddScoped<IPlaceRepository, PlaceRepository>();
                services.AddScoped<IProfileVolumeRepository, ProfileVolumeRepository>();
                services.AddScoped<IMarketingTaskRepository, MarketingTaskRepository>();
                services.AddSingleton<IProfileVolumeAmountPolicy, ProfileVolumeAmountPolicy>();
                services.AddSingleton<IPlaceQueries>(db.Places);
                db.provider = services.BuildServiceProvider();
                await using var scope = db.provider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<DataContext>().Database.EnsureCreatedAsync();
                await db.Sql("""
                    CREATE TABLE structures (
                        marketing_addr text NOT NULL, structure_number smallint NOT NULL,
                        max_places_per_profile integer NOT NULL, width smallint NOT NULL,
                        height smallint NOT NULL, display_height smallint NOT NULL, prev_required boolean NOT NULL,
                        pos_algo jsonb NOT NULL, activity jsonb, "group" text,
                        PRIMARY KEY(marketing_addr, structure_number));
                    """);
                return db;
            }
            catch { await db.DisposeAsync(); throw; }
        }
        public async Task Reset(string? activity)
        {
            await Sql("TRUNCATE marketing_tasks, places, profile_volumes, structures RESTART IDENTITY CASCADE");
            await Sql("INSERT INTO structures VALUES ('program',0,1,0,1,1,false,'{}',CAST(@activity AS jsonb),NULL),('program',1,1,2,0,2,false,'{}','{}',NULL)", new { activity });
            await InsertPlace(1, "program", 0, "owner", null, true);
            await InsertPlace(2, "program", 0, "inviter", 1, false);
            await InsertPlace(3, "program", 0, "member", 2, false);
            await Sql("SELECT setval(pg_get_serial_sequence('places','id'),100)");
        }
        public Task InsertPlace(int id, string marketing, byte number, string profile, int? parent, bool active) => Sql("""
            INSERT INTO places (id,parent_id,mp,pos_group,marketing_addr,structure_number,profile_addr,place_number,
                profile_login,"index",parent_profile_addr,parent_profile_login,parent_place_number,
                created_at,activated_at,is_active,kind,pos,filling,deep,matrix_filling)
            VALUES (@id,@parent,
                CASE WHEN @parent IS NULL THEN @mp ELSE
                    (SELECT mp || upper(lpad(to_hex(filling+1),8,'0')) FROM places WHERE id=@parent) END,
                0,@marketing,@number,@profile,1,@profile,@profile,
                (SELECT profile_addr FROM places WHERE id=@parent),(SELECT profile_login FROM places WHERE id=@parent),
                CASE WHEN @parent IS NULL THEN NULL ELSE 1 END,1,NULL,@active,0,
                COALESCE((SELECT filling+1 FROM places WHERE id=@parent),0),0,
                COALESCE((SELECT deep+1 FROM places WHERE id=@parent),1),1);
            UPDATE places SET filling=filling+1 WHERE id=@parent
            """, new { id, parent, mp = id.ToString("X8"), marketing, number = (short)number, profile, active });
        public async Task<Ardalis.Result.Result<ReferalProgram.Dto.CommandResponse>> Choose()
        {
            await using var scope = provider.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            return await new ChooseInviterCommandHandler(Places, repo, Structures, new SourcePlaceResolver(repo), context)
                .Handle(new("program", "inviter", "new-profile", 1, 1, null, "new-profile"), default);
        }
        public async Task<Ardalis.Result.Result<ReferalProgram.Dto.CommandResponse>> Activate(byte number, int key)
        {
            await using var scope = provider.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();
            return await new ActivatePlaceCommandHandler(repo, new ActivatePlacePolicy(Structures, Places, new Commands()),
                Structures, new SourcePlaceResolver(repo), scope.ServiceProvider.GetRequiredService<DataContext>())
                .Handle(new("program", number, "member", 1, key, key, null), default);
        }
        public async Task ResetPeriod(byte number)
        {
            await using var scope = provider.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();
            foreach (var place in await repo.GetStructurePlacesAsync("program", number, default))
                if (place.ParentId is not null) place.ResetActivity();
            await scope.ServiceProvider.GetRequiredService<DataContext>().SaveChangesAsync();
        }
        public async Task Sql(string sql, object? args = null)
        {
            await using var connection = await data.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, args);
        }
        public async Task<long> Scalar(string sql, object? args = null)
        {
            await using var connection = await data.OpenConnectionAsync();
            return await connection.ExecuteScalarAsync<long>(sql, args);
        }
        public Task<long> Count(string table) => Scalar($"SELECT count(*) FROM {table}");
        public async ValueTask DisposeAsync()
        {
            if (provider is not null) await provider.DisposeAsync();
            if (data is not null) await data.DisposeAsync();
            await using var command = admin.CreateCommand($"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
            await command.ExecuteNonQueryAsync();
            await admin.DisposeAsync();
        }
    }

    private sealed class Commands : IProgramCommandQueries
    {
        public Task<ProgramCommandConfiguration> GetConfigurationAsync(string marketingAddr, CancellationToken cancellationToken) =>
            Task.FromResult(new ProgramCommandConfiguration(Enumerable.Range(1,4).ToDictionary(
                n => (byte)n, _ => (IReadOnlySet<uint>)new HashSet<uint> { ProgramCommandTags.ActivatePlace })));
    }
}
