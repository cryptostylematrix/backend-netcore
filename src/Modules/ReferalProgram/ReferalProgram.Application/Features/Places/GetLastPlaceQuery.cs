namespace ReferalProgram.Application.Features.Places;

public sealed record GetLastPlaceQuery(
    string MarketingAddr,
    byte StructureNumber,
    string? ProfileAddr) : IQuery<PlaceResponse>;

internal sealed class GetLastPlaceQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetLastPlaceQuery, PlaceResponse>
{
    public async Task<Result<PlaceResponse>> Handle(
        GetLastPlaceQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetLastPlaceAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            cancellationToken);

        return place is null
            ? Result<PlaceResponse>.NotFound()
            : Result.Success(place);
    }
}
