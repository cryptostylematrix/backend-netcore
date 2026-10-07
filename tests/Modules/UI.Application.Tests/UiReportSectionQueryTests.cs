using UI.Application.Abstractions;
using UI.Application.Features.Reports;
using UI.Dto;
using Xunit;

namespace UI.Application.Tests;

public sealed class UiReportSectionQueryTests
{
    private sealed class Queries : IUiReportQueries
    {
        public readonly List<string> Calls = [];
        public UiReportFilter? Filter;
        private Task<T> Read<T>(string section, UiReportFilter filter) where T : new()
        { Calls.Add(section); Filter = filter; return Task.FromResult(new T()); }
        public Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken ct) => throw new InvalidOperationException("Whole report must not load");
        public Task<ProfileReport> GetProfilesAsync(UiReportFilter filter, CancellationToken ct) => Read<ProfileReport>("profiles", filter);
        public Task<ConnectionReport> GetTonConnectAsync(UiReportFilter filter, CancellationToken ct) => Read<ConnectionReport>("ton", filter);
        public Task<ActivityReport> GetActivityAsync(UiReportFilter filter, CancellationToken ct) => Read<ActivityReport>("activity", filter);
        public Task<PreferenceReport> GetPreferencesAsync(UiReportFilter filter, CancellationToken ct) => Read<PreferenceReport>("preferences", filter);
    }
    [Fact]
    public async Task Each_handler_reads_only_its_section()
    {
        var queries = new Queries();
        var clock = TimeProvider.System;
        var profiles = await new GetProfilesReportQueryHandler(queries, clock).Handle(new(2), default);
        Assert.True(profiles.IsSuccess);
        Assert.Equal(2, queries.Filter!.ProfilePage);
        var ton = await new GetTonConnectReportQueryHandler(queries, clock).Handle(new(false, true, false, true), default);
        Assert.True(ton.IsSuccess);
        Assert.False(queries.Filter!.GroupContract);
        Assert.True(queries.Filter.GroupWalletName);
        Assert.False(queries.Filter.GroupAppVersion);
        Assert.True(queries.Filter.GroupPlatform);
        var activity = await new GetActivityReportQueryHandler(queries, clock).Handle(new(3, "hour"), default);
        Assert.True(activity.IsSuccess);
        Assert.Equal(3, queries.Filter.ActivityPage);
        Assert.Equal(TimeSpan.FromHours(1), queries.Filter.To - queries.Filter.From);
        var preferences = await new GetPreferencesReportQueryHandler(queries, clock).Handle(new(), default);
        Assert.True(preferences.IsSuccess);
        Assert.Equal(new[] { "profiles", "ton", "activity", "preferences" }, queries.Calls);
        Assert.Equal(DateTimeKind.Utc, preferences.Value.GeneratedAt.Kind);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000001)]
    public async Task Invalid_pages_do_not_read_data(int page)
    {
        var queries = new Queries();
        Assert.False((await new GetProfilesReportQueryHandler(queries, TimeProvider.System).Handle(new(page), default)).IsSuccess);
        Assert.False((await new GetActivityReportQueryHandler(queries, TimeProvider.System).Handle(new(page), default)).IsSuccess);
        Assert.Empty(queries.Calls);
    }
    [Fact]
    public async Task Invalid_period_does_not_read_data()
    {
        var queries = new Queries();
        Assert.False((await new GetActivityReportQueryHandler(queries, TimeProvider.System).Handle(new(1, "invalid"), default)).IsSuccess);
        Assert.Empty(queries.Calls);
    }
}
