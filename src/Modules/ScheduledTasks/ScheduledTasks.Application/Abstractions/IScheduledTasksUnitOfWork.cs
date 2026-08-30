namespace ScheduledTasks.Application.Abstractions;

public interface IScheduledTasksUnitOfWork
{
    Task<bool> TrySaveChangesAsync(
        CancellationToken cancellationToken = default);
}
