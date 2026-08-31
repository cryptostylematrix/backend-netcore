using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Infrastructure.Persistence;

namespace ReferalProgram.Infrastructure.Repositories;

internal sealed class MarketingTaskRepository(DataContext dataContext)
    : IMarketingTaskRepository
{
    public Task<MarketingTask?> GetAsync(
        string marketingAddr,
        int taskKey,
        CancellationToken cancellationToken) =>
        dataContext.MarketingTasks
            .Include(task => task.Place)
            .Include(task => task.ResponseSourcePlace)
            .SingleOrDefaultAsync(
                task => task.MarketingAddr == marketingAddr
                    && task.TaskKey == taskKey,
                cancellationToken);

    public void Add(MarketingTask task) => dataContext.MarketingTasks.Add(task);

    public Task<MarketingTask?> GetFailedAsync(
        string marketingAddr,
        CancellationToken cancellationToken) =>
        dataContext.MarketingTasks
            .Where(task => task.MarketingAddr == marketingAddr
                && task.ErrorAt != null)
            .OrderByDescending(task => task.ErrorAt)
            .FirstOrDefaultAsync(cancellationToken);
}
