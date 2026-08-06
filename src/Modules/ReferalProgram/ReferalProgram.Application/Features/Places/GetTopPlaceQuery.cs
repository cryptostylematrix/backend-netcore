namespace ReferalProgram.Application.Features.Places;

public sealed record GetTopPlaceQuery(
    string MarketingAddr,
    byte StructureNumber) : IQuery<PlaceResponse>;

internal sealed class GetTopPlaceQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetTopPlaceQuery, PlaceResponse>
{
    public async Task<Result<PlaceResponse>> Handle(
        GetTopPlaceQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetRootPlaceAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);

        return place is null
            ? Result<PlaceResponse>.NotFound()
            : Result.Success(place);
    }
}
