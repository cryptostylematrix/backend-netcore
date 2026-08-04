namespace ReferalProgram.Application.Features.Places;

public sealed record GetPlacesCountQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr) : IQuery<PlacesCountResponse>;

internal sealed class GetPlacesCountQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPlacesCountQuery, PlacesCountResponse>
{
    public async Task<Result<PlacesCountResponse>> Handle(
        GetPlacesCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await placeQueries.GetPlacesCountAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            cancellationToken);

        return Result.Success(new PlacesCountResponse { Count = count });
    }
}
