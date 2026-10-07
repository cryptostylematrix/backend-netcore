using UI.Application.Abstractions;
using UI.Application.Features.Reports;
using UI.Dto;
using Xunit;

namespace UI.Application.Tests;

public sealed class UiReportQueryTests
{
    private sealed class Queries : IUiReportQueries
    {
        public UiReportFilter? Received;
        public Task<ProfileReport> GetProfilesAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<ConnectionReport> GetTonConnectAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<ActivityReport> GetActivityAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<PreferenceReport> GetPreferencesAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();

        public Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken ct)
        { Received = filter; return Task.FromResult(new UiReportResponse()); }
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2028, 3, 31, 12, 30, 0, TimeSpan.Zero);
    }
    [Theory]
    [InlineData(0, 1, "week")]
    [InlineData(1, -1, "week")]
    [InlineData(1000001, 1, "week")]
    [InlineData(1, 1, "invalid")]
    public async Task Invalid_filters_do_not_read_database(int profiles, int activity, string period)
    {
        var queries = new Queries();
        var result = await new GetUiReportQueryHandler(queries, new Clock())
            .Handle(new(profiles, activity, period, true, false, false, false), default);
        Assert.False(result.IsSuccess);
        Assert.Null(queries.Received);
    }
    [Theory]
    [InlineData("hour", "2028-03-31T11:30:00Z")]
    [InlineData("today", "2028-03-31T00:00:00Z")]
    [InlineData("week", "2028-03-24T12:30:00Z")]
    [InlineData("month", "2028-02-29T12:30:00Z")]
    [InlineData("three_months", "2027-12-31T12:30:00Z")]
    [InlineData("six_months", "2027-09-30T12:30:00Z")]
    [InlineData("year", "2027-03-31T12:30:00Z")]
    public async Task Periods_use_UTC_and_calendar_month_boundaries(string period, string expected)
    {
        var queries = new Queries();
        var clock = new Clock();
        var result = await new GetUiReportQueryHandler(queries, clock)
            .Handle(new(2, 3, period, false, true, false, true), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(DateTimeOffset.Parse(expected).UtcDateTime, queries.Received!.From);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, queries.Received.To);
        Assert.Equal(2, queries.Received.ProfilePage);
        Assert.Equal(3, queries.Received.ActivityPage);
        Assert.False(queries.Received.GroupContract);
        Assert.True(queries.Received.GroupWalletName);
        Assert.False(queries.Received.GroupAppVersion);
        Assert.True(queries.Received.GroupPlatform);
    }
}
