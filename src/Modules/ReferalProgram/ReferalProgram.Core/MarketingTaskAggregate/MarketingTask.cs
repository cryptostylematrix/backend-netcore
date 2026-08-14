using Common.Domain;

namespace ReferalProgram.Core.MarketingTaskAggregate;

public enum MarketingTaskStatus
{
    Completed
}

public sealed class MarketingTask : Entity, IAggregateRoot
{
    private MarketingTask()
    {
    }

    private MarketingTask(
        string marketingAddr,
        int taskKey,
        long taskQueryId,
        DateTimeOffset completedAt)
    {
        MarketingAddr = marketingAddr;
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        Status = MarketingTaskStatus.Completed;
        CreatedAt = completedAt;
        UpdatedAt = completedAt;
    }

    public string MarketingAddr { get; private set; } = null!;
    public int TaskKey { get; private set; }
    public long TaskQueryId { get; private set; }
    public MarketingTaskStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static MarketingTask Complete(
        string marketingAddr,
        int taskKey,
        long taskQueryId,
        DateTimeOffset completedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);

        if (taskKey <= 0)
            throw new ArgumentOutOfRangeException(nameof(taskKey));

        if (taskQueryId < 0)
            throw new ArgumentOutOfRangeException(nameof(taskQueryId));

        return new MarketingTask(
            marketingAddr,
            taskKey,
            taskQueryId,
            completedAt);
    }

    public void MarkCompleted(long taskQueryId, DateTimeOffset completedAt)
    {
        if (taskQueryId < 0)
            throw new ArgumentOutOfRangeException(nameof(taskQueryId));

        TaskQueryId = taskQueryId;
        Status = MarketingTaskStatus.Completed;
        UpdatedAt = completedAt;
    }
}
