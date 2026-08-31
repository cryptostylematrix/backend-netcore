using Common.Domain;

namespace ReferalProgram.Core.MarketingTaskAggregate;

public interface IMarketingTaskRepository : IRepository<MarketingTask>
{
    Task<MarketingTask?> GetAsync(
        string marketingAddr,
        int taskKey,
        CancellationToken cancellationToken);

    Task<MarketingTask?> GetFailedAsync(
        string marketingAddr,
        CancellationToken cancellationToken);

    void Add(MarketingTask task);
}
