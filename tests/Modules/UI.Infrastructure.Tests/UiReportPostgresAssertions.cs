using Dapper;
using Npgsql;
using UI.Application.Abstractions;
using UI.Infrastructure.Queries;
using Xunit;

namespace UI.Infrastructure.Tests;

internal static class UiReportPostgresAssertions
{
    public static async Task Verify(NpgsqlConnection admin, string appString, DateTime now)
    {
        // Only called by the disposable Docker regression database.
        await admin.ExecuteAsync("TRUNCATE wallet_profile_intents, profiles, ton_connections, wallet_preferences CASCADE");
        await using var source = NpgsqlDataSource.Create(appString);
        var queries = new UiReportQueries(source);
        var filter = new UiReportFilter(1, 1, now.AddHours(-1), now, true, false, false, false);
        var empty = await queries.GetAsync(filter, default);
        Assert.Equal(0, empty.Profiles.TotalProfiles);
        Assert.Empty(empty.TonConnect.Groups);
        Assert.Empty(empty.Preferences.Groups);
        Assert.Equal(1, empty.Activity.Page);
        await admin.ExecuteAsync("""
            INSERT INTO profiles(address,login,content,updated_at)
            SELECT 'p' || i, 'login' || i, '{}'::jsonb, @now FROM generate_series(1,43) i;
            INSERT INTO wallet_profile_intents(wallet_addr,profile_addr,mode,owned,created_at,updated_at)
            SELECT 'w' || lpad(i::text,2,'0'), 'p' || i, 'preview',false,@now - interval '2 days',@now
            FROM generate_series(1,41) i;
            -- p1 moves to w02. A later ownership refresh on w01 must not win.
            INSERT INTO wallet_profile_intents(wallet_addr,profile_addr,mode,owned,created_at,updated_at)
            VALUES ('w02','p1','preview',false,@now - interval '1 day',@now - interval '1 day'),
                   ('w02','p42','owner',true,@now,@now),
                   ('w03','p42','preview',false,@now,@now);
            -- p43 is cached but has no saved relationship and must not enter the denominator.
            INSERT INTO ton_connections(wallet_addr,contract_version,wallet_name,app_version,platform,created_at,updated_at,last_connected_at)
            SELECT 'w' || lpad(i::text,2,'0'), CASE WHEN i <= 21 THEN 'v4r2' ELSE 'v5r1' END,
                'Wallet', CASE WHEN i <= 10 THEN '1' ELSE '2' END,
                CASE WHEN i % 2 = 0 THEN 'android' ELSE 'ios' END,@now,@now,@now - i * interval '1 minute'
            FROM generate_series(1,41) i;
            INSERT INTO ton_connections(wallet_addr,contract_version,wallet_name,app_version,platform,created_at,updated_at,last_connected_at)
            VALUES ('old','unknown version','Legacy','1','browser',@now,@now,@now - interval '2 hours'),
                   ('future','v5r1','Wallet','2','ios',@now,@now,@now + interval '1 minute'),
                   ('missing','v4r2','Legacy','1','browser',@now,@now,NULL),
                   ('boundary','v4r2','Wallet','2','ios',@now,@now,@now - interval '1 hour');
            INSERT INTO wallet_preferences(wallet_addr,language,updated_at)
            VALUES ('w01','en',@now),('w02','en',@now),('w03','zh-hant',@now);
            """, new { now });
        var first = await queries.GetAsync(filter, default);
        Assert.Equal(40, first.Profiles.TotalWallets);
        Assert.Equal(42, first.Profiles.TotalProfiles);
        Assert.Equal(10, first.Profiles.Items.Count);
        Assert.Equal("w02", first.Profiles.Items[0].WalletAddr);
        Assert.Equal(2, first.Profiles.Items[0].ProfileCount);
        Assert.Equal("w03", first.Profiles.Items[1].WalletAddr); // id breaks equal created_at.
        Assert.Equal(2, first.Profiles.Items[1].ProfileCount);
        Assert.InRange(first.Profiles.Items[0].Percentage, 4.7619m, 4.762m);
        Assert.DoesNotContain(first.Profiles.Items, x => x.WalletAddr == "w01");
        Assert.Equal(42, first.Activity.Total);
        Assert.Equal(10, first.Activity.Items.Count);
        Assert.Equal("w01", first.Activity.Items[0].WalletAddr);
        Assert.Equal(now.AddMinutes(-1), first.Activity.Items[0].LastConnectedAt);
        Assert.Equal(45, first.TonConnect.Total);
        Assert.Equal(3, first.TonConnect.Groups.Count);
        Assert.Equal(3, first.Preferences.Total);
        Assert.Equal(2, first.Preferences.Groups.Single(x => x.Language == "en").Count);
        Assert.Contains(first.Preferences.Groups, x => x.Language == "zh-hant");
        var second = await queries.GetAsync(filter with { ProfilePage = 2, ActivityPage = 2 }, default);
        Assert.Equal(10, second.Profiles.Items.Count);
        Assert.Empty(first.Profiles.Items.Select(x => x.WalletAddr).Intersect(second.Profiles.Items.Select(x => x.WalletAddr)));
        Assert.Equal(10, first.Profiles.PageSize);
        Assert.Equal(10, first.Activity.PageSize);
        var third = await queries.GetAsync(filter with { ProfilePage = 3 }, default);
        var fourth = await queries.GetAsync(filter with { ProfilePage = 4 }, default);
        var all = first.Profiles.Items.Concat(second.Profiles.Items).Concat(third.Profiles.Items).Concat(fourth.Profiles.Items).ToArray();
        Assert.Equal(40, all.Select(x => x.WalletAddr).Distinct().Count());
        Assert.InRange(all.Sum(x => x.Percentage), 99.999m, 100.001m);
        var last = await queries.GetAsync(filter with { ProfilePage = 999, ActivityPage = 999 }, default);
        Assert.Equal(4, last.Profiles.Page);
        Assert.Equal(5, last.Activity.Page);
        Assert.Equal(2, last.Activity.Items.Count);
        Assert.Equal("boundary", last.Activity.Items[^1].WalletAddr);
        for (var flags = 0; flags < 16; flags++)
        {
            var report = await queries.GetAsync(filter with { GroupContract = (flags & 1) != 0,
                GroupWalletName = (flags & 2) != 0, GroupAppVersion = (flags & 4) != 0, GroupPlatform = (flags & 8) != 0 }, default);
            Assert.Equal(45, report.TonConnect.Groups.Sum(x => x.Count));
            Assert.InRange(report.TonConnect.Groups.Sum(x => x.Percentage), 99.999m, 100.001m);
            if (flags == 0) Assert.Single(report.TonConnect.Groups);
            Assert.All(report.TonConnect.Groups, x => {
                Assert.Equal((flags & 1) == 0, x.ContractVersion is null);
                Assert.Equal((flags & 2) == 0, x.WalletName is null);
                Assert.Equal((flags & 4) == 0, x.AppVersion is null);
                Assert.Equal((flags & 8) == 0, x.Platform is null);
            });
        }
    }
}
