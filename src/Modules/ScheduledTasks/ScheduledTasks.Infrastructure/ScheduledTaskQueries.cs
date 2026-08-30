using Microsoft.EntityFrameworkCore;
using ScheduledTasks.Application.Abstractions;
using ScheduledTasks.Core.TaskAggregate;
using ScheduledTasks.Infrastructure.Persistence;

namespace ScheduledTasks.Infrastructure;

internal sealed class ScheduledTaskQueries(ScheduledTasksDataContext dataContext)
    : IScheduledTaskQueries
{
    public async Task<IReadOnlyCollection<string>> GetDueTaskCommandDocumentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        await dataContext.Tasks
            .AsNoTracking()
            .Where(task => task.ExecuteAtUtc != null
                && (task.Status == ScheduledTaskStatus.Error
                    || (task.Status == ScheduledTaskStatus.Active
                        && task.ExecuteAtUtc <= nowUtc)))
            .Select(task => task.Commands)
            .ToListAsync(cancellationToken);
}
