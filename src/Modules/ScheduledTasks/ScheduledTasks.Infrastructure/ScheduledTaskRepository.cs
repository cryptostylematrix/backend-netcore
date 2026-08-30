using Microsoft.EntityFrameworkCore;
using ScheduledTasks.Core.TaskAggregate;
using ScheduledTasks.Infrastructure.Persistence;

namespace ScheduledTasks.Infrastructure;

internal sealed class ScheduledTaskRepository(ScheduledTasksDataContext dataContext)
    : IScheduledTaskRepository
{
    public Task<ScheduledTask?> GetNextDueAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        dataContext.Tasks
            .Where(task => task.Status == ScheduledTaskStatus.Active
                && task.ExecuteAtUtc != null
                && task.ExecuteAtUtc <= nowUtc)
            .OrderBy(task => task.ExecuteAtUtc)
            .ThenBy(task => task.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
