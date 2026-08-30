using ScheduledTasks.Application;
using ScheduledTasks.Application.Abstractions;

namespace ScheduledTasks.Application.Tests;

public sealed class MarketingTaskBlockerTests
{
    [Fact]
    public async Task Blocks_only_the_marketing_referenced_by_candidate_commands()
    {
        var queries = new StubQueries(
            """
            [{
              "module":"program",
              "type":"program.structure.compress",
              "target":{"marketingAddress":"EQ_BLOCKED"},
              "arguments":{"structureNumber":1}
            }]
            """);
        var blocker = new MarketingTaskBlocker(
            queries,
            new TaskCommandDocumentParser(),
            new FixedTimeProvider(DateTimeOffset.Parse("2030-01-01T00:00:00Z")));

        Assert.True(await blocker.IsBlockedAsync("EQ_BLOCKED", default));
        Assert.False(await blocker.IsBlockedAsync("EQ_OTHER", default));
    }

    private sealed class StubQueries(params string[] documents)
        : IScheduledTaskQueries
    {
        public Task<IReadOnlyCollection<string>> GetDueTaskCommandDocumentsAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<string>>(documents);

    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
