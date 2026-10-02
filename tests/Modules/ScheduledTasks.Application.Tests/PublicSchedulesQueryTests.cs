using Ardalis.Result;
using ScheduledTasks.Application;
using ScheduledTasks.Application.Abstractions;
using Xunit;

namespace ScheduledTasks.Application.Tests;

public sealed class PublicSchedulesQueryTests
{
    [Fact]
    public async Task Reads_only_schedules_and_propagates_filter_and_cancellation()
    {
        var source = new Schedules();
        using var cts = new CancellationTokenSource();
        var result = await new GetPublicSchedulesQueryHandler(source).Handle(new(" sample ", " selected ", " item "), cts.Token);
        Assert.True(result.IsSuccess);
        Assert.Equal(new ScheduleFilter("sample", "selected", "item"), source.Filter);
        Assert.Equal(cts.Token, source.Token);
        Assert.Same(source.Rows, result.Value);
    }

    [Fact]
    public async Task Empty_scope_is_invalid_and_does_not_read_database()
    {
        var source = new Schedules();
        var result = await new GetPublicSchedulesQueryHandler(source).Handle(new("sample", " ", "item"), default);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Null(source.Filter);
    }

    [Fact]
    public async Task Failure_is_not_returned_as_an_empty_schedule()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GetPublicSchedulesQueryHandler(new Schedules { Fail = true }).Handle(new("sample", "selected", "item"), default));
    }

    [Fact]
    public async Task Requested_cancellation_is_propagated()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetPublicSchedulesQueryHandler(new Schedules()).Handle(new("sample", "selected", "item"), cts.Token));
    }

    private sealed class Schedules : IPublicScheduleQueries
    {
        public ScheduleFilter? Filter { get; private set; }
        public CancellationToken Token { get; private set; }
        public bool Fail { get; init; }
        public IReadOnlyList<PublicSchedule> Rows { get; } = [];
        public Task<IReadOnlyList<PublicSchedule>> GetAsync(ScheduleFilter filter, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (Fail) throw new InvalidOperationException("Unavailable");
            Filter = filter;
            Token = ct;
            return Task.FromResult(Rows);
        }
    }
}
