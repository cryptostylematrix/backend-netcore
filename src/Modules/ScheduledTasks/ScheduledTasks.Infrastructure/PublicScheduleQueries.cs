using IntegrationRequests.Scheduling;
using Microsoft.EntityFrameworkCore;
using ScheduledTasks.Application.Abstractions;
using ScheduledTasks.Application;
using ScheduledTasks.Infrastructure.Persistence;

namespace ScheduledTasks.Infrastructure;

internal sealed class PublicScheduleQueries(ScheduledTasksDataContext context, IEnumerable<IPublicTaskCommandDescriptor> descriptors)
    : IPublicScheduleQueries
{
    public async Task<IReadOnlyList<PublicSchedule>> GetAsync(
        ScheduleFilter filter, CancellationToken cancellationToken)
    {
        var descriptor = descriptors.SingleOrDefault(value => value.Module == filter.Module);
        if (descriptor is null) return [];
        var target = descriptor.BuildCommandFilter(filter.Scope);
        var rows = await context.Tasks.AsNoTracking()
            .Where(task => EF.Functions.JsonContains(task.Commands, target))
            .OrderBy(task => task.ExecuteAtUtc).ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(row => PublicScheduleProjection.Create(row, filter, descriptor))
            .OfType<PublicSchedule>().ToArray();
    }
}
