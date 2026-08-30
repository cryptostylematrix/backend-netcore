namespace ScheduledTasks.Application.Abstractions;

public interface IScheduledTaskQueries
{
    Task<IReadOnlyCollection<string>> GetDueTaskCommandDocumentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
