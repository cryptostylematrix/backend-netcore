using Microsoft.EntityFrameworkCore;
using Npgsql;
using UI.Core.TonConnectionAggregate;
using UI.Core.WalletPreferencesAggregate;
using UI.Infrastructure.Persistence;
using UI.Infrastructure.Repositories;
using Xunit;

namespace UI.Infrastructure.Tests;

public sealed class TonConnectionPostgresTests
{
    [PostgresFact]
    public async Task Migrations_and_repository_preserve_connection_timestamps_on_postgres()
    {
        var port = Environment.GetEnvironmentVariable("TON_CONNECTION_TEST_POSTGRES_PORT");
        Assert.False(string.IsNullOrWhiteSpace(port));
        var adminString = $"Host=127.0.0.1;Port={int.Parse(port)};Database=ton_connection_tests;Username=postgres;Pooling=false";
        await using var admin = new NpgsqlConnection(adminString);
        await admin.OpenAsync();
        await Execute(admin, "CREATE ROLE ton_connection_app LOGIN");
        var scripts = FindScripts();
        await Execute(admin, File.ReadAllText(Path.Combine(scripts, "003_create_ton_connections.sql"))
            .Replace("v_database_username text := '';", "v_database_username text := 'ton_connection_app';"));
        await Execute(admin, """
            INSERT INTO ton_connections
                (wallet_addr, contract_version, wallet_name, app_version, platform, created_at, updated_at)
            VALUES ('legacy', 'v4r2', 'Wallet', '1', 'browser', '2026-01-01Z', '2026-01-01Z')
            """);
        await Execute(admin, File.ReadAllText(Path.Combine(scripts, "004_add_last_connected_at.sql")));
        await Execute(admin, File.ReadAllText(Path.Combine(scripts, "005_create_wallet_preferences.sql"))
            .Replace("v_database_username text := '';", "v_database_username text := 'ton_connection_app';"));
        await Execute(admin, File.ReadAllText(Path.Combine(scripts, "006_allow_extensible_language_tags.sql")));
        var appString = adminString.Replace("Username=postgres", "Username=ton_connection_app");
        DataContext Context() => new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(appString).Options);
        var start = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        async Task Save(string address, DateTime time, string version = "1", string name = "Wallet",
            string platform = "browser", string contract = "v4r2")
        {
            await using var context = Context();
            await new TonConnectionRepository(context).UpsertAsync(
                TonConnection.Create(address, contract, name, version, platform, time), default);
        }
        async Task<TonConnection> Read(string address)
        {
            await using var context = Context();
            return await context.TonConnections.AsNoTracking().SingleAsync(x => x.WalletAddr == address);
        }

        Assert.Null((await Read("legacy")).LastConnectedAtUtc);
        await Save("legacy", start);
        Assert.Equal(start, (await Read("legacy")).LastConnectedAtUtc);

        await Save("new", start);
        var first = await Read("new");
        Assert.Equal(start, first.CreatedAtUtc);
        Assert.Equal(start, first.UpdatedAtUtc);
        Assert.Equal(start, first.LastConnectedAtUtc);

        await Save("new", start.AddMinutes(1));
        var repeated = await Read("new");
        Assert.Equal(start, repeated.CreatedAtUtc);
        Assert.Equal(start, repeated.UpdatedAtUtc);
        Assert.Equal(start.AddMinutes(1), repeated.LastConnectedAtUtc);

        await Save("new", start.AddMinutes(2), "2", "Other Wallet", "android", "v5r1");
        var changed = await Read("new");
        Assert.Equal(start, changed.CreatedAtUtc);
        Assert.Equal(start.AddMinutes(2), changed.UpdatedAtUtc);
        Assert.Equal(start.AddMinutes(2), changed.LastConnectedAtUtc);
        Assert.Equal("Other Wallet", changed.WalletName);
        Assert.Equal("2", changed.AppVersion);
        Assert.Equal("android", changed.Platform);
        Assert.Equal("v5r1", changed.ContractVersion);

        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => Save("concurrent", start.AddSeconds(i))));
        Assert.Equal(start.AddSeconds(11), (await Read("concurrent")).LastConnectedAtUtc);
        await Save("concurrent", start.AddSeconds(1));
        Assert.Equal(start.AddSeconds(11), (await Read("concurrent")).LastConnectedAtUtc);
        async Task<string> Language(string address, string language, bool overwrite, DateTime time)
        {
            await using var context = Context();
            return await new WalletPreferencesRepository(context).SaveLanguageAsync(
                WalletPreferences.Create(address, language, time), overwrite, default);
        }
        // First migration imports browser data; later migration requests cannot overwrite the DB.
        Assert.Equal("ru", await Language("A", "ru", false, start));
        Assert.Equal("ru", await Language("A", "de", false, start.AddMinutes(1)));
        Assert.Equal("fr", await Language("B", "fr", false, start));
        Assert.Equal("uk", await Language("A", "uk", true, start.AddMinutes(2)));
        Assert.Equal("uk", await Language("A", "es", false, start.AddMinutes(3)));
        Assert.Equal("uk", await Language("A", "uk", true, start.AddMinutes(4)));
        Assert.Equal("ja", await Language("future", "ja", false, start));
        Assert.Equal("zh-hant", await Language("future", "zh-hant", true, start.AddMinutes(1)));
        Assert.Equal("zh-hant", await Language("future", "en", false, start.AddMinutes(2)));
        var initialized = await Task.WhenAll(Enumerable.Range(0, 12).Select(i =>
            Language("race", i % 2 == 0 ? "en" : "pt", false, start)));
        Assert.Single(initialized.Distinct());
        await using var final = Context();
        Assert.Equal(1, await final.TonConnections.CountAsync(x => x.WalletAddr == "concurrent"));
        Assert.Equal(1, await final.WalletPreferences.CountAsync(x => x.WalletAddr == "race"));
        var preference = await final.WalletPreferences.SingleAsync(x => x.WalletAddr == "A");
        Assert.Equal("uk", preference.Language);
        Assert.Equal(start.AddMinutes(2), preference.UpdatedAtUtc);
        Assert.Equal("fr", (await final.WalletPreferences.SingleAsync(x => x.WalletAddr == "B")).Language);
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TON_CONNECTION_TEST_POSTGRES_PORT")))
                Skip = "Run Postgres/run.sh to use a disposable Docker database.";
        }
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string FindScripts()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "Modules", "UI", "Database", "Scripts");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException("UI SQL scripts not found");
    }
}
