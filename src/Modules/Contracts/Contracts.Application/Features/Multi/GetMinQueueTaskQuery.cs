using Contracts.Domain.Aggregates.Multi;

namespace Contracts.Application.Features.Multi;

public sealed record GetMinQueueTaskQuery() : IQuery<MinQueueTaskResponse>;

internal sealed class GetMinQueueTaskQueryHandler(ITonClient tonClient, IMapper mapper, MultiContract multiContract) : 
    IQueryHandler<GetMinQueueTaskQuery, MinQueueTaskResponse>
{
    public async Task<Result<MinQueueTaskResponse>> Handle(GetMinQueueTaskQuery request, CancellationToken ct)
    {
        var result = await multiContract.GetMinQueueTask(tonClient, ct);

        if (!result.IsSuccess)
        {
            return Result<MinQueueTaskResponse>.Error(new ErrorList(result.Errors));
        }
        
        var response = mapper.Map<MinQueueTaskResponse>(result.Value);
        return Result.Success(response);
    }
}