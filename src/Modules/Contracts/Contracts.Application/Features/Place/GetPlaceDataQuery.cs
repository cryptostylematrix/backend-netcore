using Contracts.Domain.Aggregates.Place;

namespace Contracts.Application.Features.Place;

public sealed record GetPlaceDataQuery(string Addr) : IQuery<PlaceDataResponse>;
 
internal sealed class GetPlaceDataQueryHandler(ITonClient tonClient, IMapper mapper) : 
    IQueryHandler<GetPlaceDataQuery, PlaceDataResponse>
{
    public async Task<Result<PlaceDataResponse>> Handle(GetPlaceDataQuery request, CancellationToken ct)
    {
        var inviteContract = PlaceContract.CreateFromAddress(new Address(request.Addr));
        var result = await inviteContract.GetPlaceData(tonClient, ct);
 
        if (!result.IsSuccess)
        {
            return Result<PlaceDataResponse>.Error(new ErrorList(result.Errors));
        }
         
        var response = mapper.Map<PlaceDataResponse>(result.Value);
        return Result.Success(response);
    }
}