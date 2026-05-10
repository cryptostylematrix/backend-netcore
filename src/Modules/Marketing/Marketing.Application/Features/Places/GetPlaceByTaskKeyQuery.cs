namespace Marketing.Application.Features.Places;


public sealed record GetPlaceByTaskKeyQuery(string MarketingAddr, uint TaskKey)
    : IQuery<PlaceResponse>;

internal sealed class GetPlaceByTaskKeyQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPlaceByTaskKeyQuery, PlaceResponse>
{
    public async Task<Result<PlaceResponse>> Handle(
        GetPlaceByTaskKeyQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetPlaceByTaskKeyAsync(
            marketingAddr: request.MarketingAddr,
            taskKey: request.TaskKey,
            cancellationToken);
        
        return place is null ? 
            Result<PlaceResponse>.NotFound() : 
            Result.Success(place);
    }
}