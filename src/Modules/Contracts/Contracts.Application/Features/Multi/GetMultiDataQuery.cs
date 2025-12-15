using Contracts.Domain.Aggregates.Multi;

namespace Contracts.Application.Features.Multi;

public sealed record GetMultiDataQuery() : IQuery<MultiDataResponse>;

internal sealed class GetMultiDataQueryHandler(ITonClient tonClient, IMapper mapper, MultiContract multiContract) : 
    IQueryHandler<GetMultiDataQuery, MultiDataResponse>
{
    public async Task<Result<MultiDataResponse>> Handle(GetMultiDataQuery request, CancellationToken ct)
    {
        var result = await multiContract.GetMultiData(tonClient, ct);

        if (!result.IsSuccess)
        {
            return Result<MultiDataResponse>.Error(new ErrorList(result.Errors));
        }
        
        var response = mapper.Map<MultiDataResponse>(result.Value);
        return Result.Success(response);
    }
}