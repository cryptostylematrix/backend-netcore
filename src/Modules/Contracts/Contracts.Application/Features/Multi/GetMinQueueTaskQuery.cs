namespace Contracts.Application.Features.Multi;

public sealed record GetMinQueueTaskQuery() : IQuery<MinQueueTaskResponse>;

internal sealed class GetMinQueueTaskQueryHandler(IMultiQueries queries)
    : IQueryHandler<GetMinQueueTaskQuery, MinQueueTaskResponse>
{
    public Task<Result<MinQueueTaskResponse>> Handle(GetMinQueueTaskQuery request, CancellationToken ct)
        => queries.GetMinQueueTaskAsync(ct);
}