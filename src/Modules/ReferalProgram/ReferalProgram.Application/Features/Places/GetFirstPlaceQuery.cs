namespace ReferalProgram.Application.Features.Places;

public sealed record GetFirstPlaceQuery(
    string MarketingAddr,
    byte StructureNumber,
    string? ProfileAddr) : IQuery<PlaceResponse>;

internal sealed class GetFirstPlaceQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetFirstPlaceQuery, PlaceResponse>
{
    public async Task<Result<PlaceResponse>> Handle(
        GetFirstPlaceQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetFirstPlaceAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            cancellationToken);

        return place is null
            ? Result<PlaceResponse>.NotFound()
            : Result.Success(place);
    }
}
