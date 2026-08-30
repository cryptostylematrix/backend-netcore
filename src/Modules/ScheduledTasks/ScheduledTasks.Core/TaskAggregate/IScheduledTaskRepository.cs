namespace ScheduledTasks.Core.TaskAggregate;

public interface IScheduledTaskRepository
{
    Task<ScheduledTask?> GetNextDueAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
