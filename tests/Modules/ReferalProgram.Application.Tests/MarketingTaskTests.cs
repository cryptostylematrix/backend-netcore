using Common.Domain;
using ReferalProgram.Application.Features.MarketingTasks;
using ReferalProgram.Core.MarketingTaskAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class MarketingTaskTests
{
    [Fact]
    public void Complete_creates_completed_task_with_composite_identity()
    {
        var completedAt = DateTimeOffset.Parse("2026-08-14T10:00:00Z");

        var task = MarketingTask.Complete("marketing", 12, 34, completedAt);

        Assert.Equal("marketing", task.MarketingAddr);
        Assert.Equal(12, task.TaskKey);
        Assert.Equal(34, task.TaskQueryId);
        Assert.Equal(MarketingTaskStatus.Completed, task.Status);
        Assert.Equal(completedAt, task.CreatedAt);
        Assert.Equal(completedAt, task.UpdatedAt);
    }

    [Fact]
    public void MarkCompleted_updates_diagnostic_query_id_without_changing_identity()
    {
        var createdAt = DateTimeOffset.Parse("2026-08-14T10:00:00Z");
        var updatedAt = createdAt.AddMinutes(1);
        var task = MarketingTask.Complete("marketing", 12, 34, createdAt);

        task.MarkCompleted(56, updatedAt);

        Assert.Equal("marketing", task.MarketingAddr);
        Assert.Equal(12, task.TaskKey);
        Assert.Equal(56, task.TaskQueryId);
        Assert.Equal(createdAt, task.CreatedAt);
        Assert.Equal(updatedAt, task.UpdatedAt);
    }

    [Fact]
    public async Task Processed_query_uses_repository_identity()
    {
        var repository = new Repository
        {
            Existing = MarketingTask.Complete(
                "marketing",
                12,
                34,
                DateTimeOffset.UtcNow)
        };
        var handler = new IsMarketingTaskProcessedQueryHandler(repository);

        var result = await handler.Handle(
            new IsMarketingTaskProcessedQuery("marketing", 12),
            default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(("marketing", 12), repository.LastLookup);
    }

    [Fact]
    public async Task Mark_command_adds_and_saves_new_completed_task()
    {
        var repository = new Repository();
        var unitOfWork = new UnitOfWork();
        var handler = new MarkMarketingTaskProcessedCommandHandler(
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new MarkMarketingTaskProcessedCommand("marketing", 12, 34),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.Added);
        Assert.Equal("marketing", repository.Added.MarketingAddr);
        Assert.Equal(12, repository.Added.TaskKey);
        Assert.Equal(34, repository.Added.TaskQueryId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private sealed class Repository : IMarketingTaskRepository
    {
        public MarketingTask? Existing { get; init; }
        public MarketingTask? Added { get; private set; }
        public (string MarketingAddr, int TaskKey)? LastLookup { get; private set; }

        public Task<MarketingTask?> GetAsync(
            string marketingAddr,
            int taskKey,
            CancellationToken cancellationToken)
        {
            LastLookup = (marketingAddr, taskKey);
            return Task.FromResult(Existing);
        }

        public void Add(MarketingTask task) => Added = task;
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }
}
