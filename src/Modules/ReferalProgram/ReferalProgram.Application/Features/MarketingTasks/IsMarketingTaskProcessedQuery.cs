using ReferalProgram.Core.MarketingTaskAggregate;

namespace ReferalProgram.Application.Features.MarketingTasks;

public sealed record IsMarketingTaskProcessedQuery(
    string MarketingAddr,
    int TaskKey) : IQuery<bool>;

internal sealed class IsMarketingTaskProcessedQueryHandler(
    IMarketingTaskRepository repository)
    : IQueryHandler<IsMarketingTaskProcessedQuery, bool>
{
    public async Task<Result<bool>> Handle(
        IsMarketingTaskProcessedQuery request,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(
            request.MarketingAddr,
            request.TaskKey,
            cancellationToken);

        return Result.Success(task?.Status == MarketingTaskStatus.Completed);
    }
}
