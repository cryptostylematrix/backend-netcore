using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Application.Mappings;

namespace ReferalProgram.Application.Features.MarketingTasks;

public sealed record GetMarketingTaskQuery(
    string MarketingAddr,
    int TaskKey) : IQuery<MarketingTaskReceipt?>;

public sealed record MarketingTaskReceipt(
    long TaskQueryId,
    CommandResponse CommandResponse,
    DateTimeOffset? ResponseAttemptedAt,
    DateTimeOffset? ErrorAt,
    string? ErrorReason);

internal sealed class GetMarketingTaskQueryHandler(
    IMarketingTaskRepository repository)
    : IQueryHandler<GetMarketingTaskQuery, MarketingTaskReceipt?>
{
    public async Task<Result<MarketingTaskReceipt?>> Handle(
        GetMarketingTaskQuery request,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(
            request.MarketingAddr,
            request.TaskKey,
            cancellationToken);

        if (task is null)
            return Result.Success<MarketingTaskReceipt?>(null);

        var response = new CommandResponse(
            task.ResponseCode,
            PlaceResponseMapper.Map(task.ResponseSourcePlace));

        return Result.Success<MarketingTaskReceipt?>(new MarketingTaskReceipt(
            task.TaskQueryId,
            response,
            task.ResponseAttemptedAt,
            task.ErrorAt,
            task.ErrorReason));
    }

}
