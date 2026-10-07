using System.Data;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using UI.Application.Abstractions;
using UI.Dto;

namespace UI.Infrastructure.Queries;

internal sealed class UiReportQueries([FromKeyedServices("UI")] NpgsqlDataSource dataSource) : IUiReportQueries
{
    public Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken ct) =>
        ReadAsync(async (connection, transaction) => new UiReportResponse
        {
            GeneratedAt = filter.To,
            Profiles = await ReadProfiles(connection, transaction, filter, ct),
            TonConnect = await ReadTonConnect(connection, transaction, filter, ct),
            Activity = await ReadActivity(connection, transaction, filter, ct),
            Preferences = await ReadPreferences(connection, transaction, filter, ct)
        }, ct);

    public Task<ProfileReport> GetProfilesAsync(UiReportFilter filter, CancellationToken ct) =>
        ReadAsync((connection, transaction) => ReadProfiles(connection, transaction, filter, ct), ct);

    private static async Task<ProfileReport> ReadProfiles(NpgsqlConnection connection, NpgsqlTransaction transaction,
        UiReportFilter filter, CancellationToken ct)
    {
        CommandDefinition Command(string sql, object? parameters = null) => new(sql, parameters, transaction,
            cancellationToken: ct);
        // A profile belongs to its latest currently saved link, not its latest ownership refresh.
        const string profileGroups = """
            WITH latest AS (
                SELECT DISTINCT ON (profile_addr) wallet_addr, profile_addr
                FROM public.wallet_profile_intents
                ORDER BY profile_addr, created_at DESC, id DESC
            ), grouped AS (
                SELECT wallet_addr, COUNT(*) AS profile_count FROM latest GROUP BY wallet_addr
            )
            """;
        var totals = await connection.QuerySingleAsync<ProfileTotals>(Command(profileGroups + """
            SELECT COUNT(*) AS "Wallets", COALESCE(SUM(profile_count), 0)::bigint AS "Profiles" FROM grouped;
            """));
        var profilePage = ClampPage(filter.ProfilePage, totals.Wallets);
        var profiles = (await connection.QueryAsync<ProfileReportRow>(Command(profileGroups + """
            SELECT wallet_addr AS "WalletAddr", profile_count AS "ProfileCount",
                100.0 * profile_count / NULLIF(@total, 0) AS "Percentage"
            FROM grouped ORDER BY profile_count DESC, wallet_addr
            LIMIT 10 OFFSET @offset;
            """, new { total = totals.Profiles, offset = (profilePage - 1) * 10 }))).ToArray();
        return new() { TotalWallets = totals.Wallets, TotalProfiles = totals.Profiles, Page = profilePage, Items = profiles };
    }

    public Task<ConnectionReport> GetTonConnectAsync(UiReportFilter filter, CancellationToken ct) =>
        ReadAsync((connection, transaction) => ReadTonConnect(connection, transaction, filter, ct), ct);

    private static async Task<ConnectionReport> ReadTonConnect(NpgsqlConnection connection, NpgsqlTransaction transaction,
        UiReportFilter filter, CancellationToken ct)
    {
        CommandDefinition Command(string sql, object? parameters = null) => new(sql, parameters, transaction,
            cancellationToken: ct);
        var groups = (await connection.QueryAsync<ConnectionReportGroup>(Command("""
            WITH grouped AS (
                SELECT CASE WHEN @GroupContract THEN contract_version END AS contract_version,
                    CASE WHEN @GroupWalletName THEN wallet_name END AS wallet_name,
                    CASE WHEN @GroupAppVersion THEN app_version END AS app_version,
                    CASE WHEN @GroupPlatform THEN platform END AS platform,
                    COUNT(*) AS count
                FROM public.ton_connections GROUP BY 1, 2, 3, 4
            )
            SELECT contract_version AS "ContractVersion", wallet_name AS "WalletName",
                app_version AS "AppVersion", platform AS "Platform", count AS "Count",
                100.0 * count / NULLIF(SUM(count) OVER (), 0) AS "Percentage"
            FROM grouped ORDER BY count DESC, contract_version, wallet_name, app_version, platform;
            """, new { filter.GroupContract, filter.GroupWalletName, filter.GroupAppVersion, filter.GroupPlatform }))).ToArray();
        return new() { Total = groups.Sum(x => x.Count), Groups = groups };
    }

    public Task<ActivityReport> GetActivityAsync(UiReportFilter filter, CancellationToken ct) =>
        ReadAsync((connection, transaction) => ReadActivity(connection, transaction, filter, ct), ct);

    private static async Task<ActivityReport> ReadActivity(NpgsqlConnection connection, NpgsqlTransaction transaction,
        UiReportFilter filter, CancellationToken ct)
    {
        CommandDefinition Command(string sql, object? parameters = null) => new(sql, parameters, transaction,
            cancellationToken: ct);
        var activityTotal = await connection.ExecuteScalarAsync<long>(Command("""
            SELECT COUNT(*) FROM public.ton_connections
            WHERE last_connected_at >= @From AND last_connected_at <= @To;
            """, new { filter.From, filter.To }));
        var activityPage = ClampPage(filter.ActivityPage, activityTotal);
        var active = (await connection.QueryAsync<ActivityReportRow>(Command("""
            SELECT wallet_addr AS "WalletAddr", last_connected_at AS "LastConnectedAt"
            FROM public.ton_connections WHERE last_connected_at >= @From AND last_connected_at <= @To
            ORDER BY last_connected_at DESC, wallet_addr LIMIT 10 OFFSET @offset;
            """, new { filter.From, filter.To, offset = (activityPage - 1) * 10 }))).ToArray();
        return new() { Total = activityTotal, From = filter.From, To = filter.To, Page = activityPage, Items = active };
    }

    public Task<PreferenceReport> GetPreferencesAsync(UiReportFilter filter, CancellationToken ct) =>
        ReadAsync((connection, transaction) => ReadPreferences(connection, transaction, filter, ct), ct);

    private static async Task<PreferenceReport> ReadPreferences(NpgsqlConnection connection, NpgsqlTransaction transaction,
        UiReportFilter filter, CancellationToken ct)
    {
        CommandDefinition Command(string sql, object? parameters = null) => new(sql, parameters, transaction,
            cancellationToken: ct);
        var preferences = (await connection.QueryAsync<PreferenceReportGroup>(Command("""
            SELECT language AS "Language", COUNT(*) AS "Count",
                100.0 * COUNT(*) / NULLIF(SUM(COUNT(*)) OVER (), 0) AS "Percentage"
            FROM public.wallet_preferences GROUP BY language ORDER BY COUNT(*) DESC, language;
            """))).ToArray();
        return new() { Total = preferences.Sum(x => x.Count), Groups = preferences };
    }

    private async Task<T> ReadAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> read, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var result = await read(connection, transaction);
        await transaction.CommitAsync(ct);
        return result;
    }
    private static int ClampPage(int requested, long total) =>
        (int)Math.Min(Math.Max(1, requested), Math.Max(1, (total + 9) / 10));
    private sealed class ProfileTotals
    {
        public long Wallets { get; init; }
        public long Profiles { get; init; }
    }
}
