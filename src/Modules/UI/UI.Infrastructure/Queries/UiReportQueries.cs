using System.Data;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using UI.Application.Abstractions;
using UI.Dto;

namespace UI.Infrastructure.Queries;

internal sealed class UiReportQueries([FromKeyedServices("UI")] NpgsqlDataSource dataSource) : IUiReportQueries
{
    public async Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
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
        var preferences = (await connection.QueryAsync<PreferenceReportGroup>(Command("""
            SELECT language AS "Language", COUNT(*) AS "Count",
                100.0 * COUNT(*) / NULLIF(SUM(COUNT(*)) OVER (), 0) AS "Percentage"
            FROM public.wallet_preferences GROUP BY language ORDER BY COUNT(*) DESC, language;
            """))).ToArray();
        await transaction.CommitAsync(ct);
        return new UiReportResponse
        {
            GeneratedAt = filter.To,
            Profiles = new() { TotalWallets = totals.Wallets, TotalProfiles = totals.Profiles,
                Page = profilePage, Items = profiles },
            TonConnect = new() { Total = groups.Sum(x => x.Count), Groups = groups },
            Activity = new() { Total = activityTotal, From = filter.From, To = filter.To,
                Page = activityPage, Items = active },
            Preferences = new() { Total = preferences.Sum(x => x.Count), Groups = preferences }
        };
    }
    private static int ClampPage(int requested, long total) =>
        (int)Math.Min(Math.Max(1, requested), Math.Max(1, (total + 9) / 10));
    private sealed class ProfileTotals
    {
        public long Wallets { get; init; }
        public long Profiles { get; init; }
    }
}
