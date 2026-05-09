namespace Marketing.Application.Features.Places;

public sealed record GetCountQuery(string MarketingAddr, byte M, string ProfileAddr) : IQuery<PlacesCountResponse>;

internal sealed class GetCountQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetCountQuery, PlacesCountResponse>
{
    public async Task<Result<PlacesCountResponse>> Handle(GetCountQuery request, CancellationToken cancellationToken)
    {
        var count = await placeQueries.GetPlacesCountAsync(
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr,
            cancellationToken);

        return Result.Success(new PlacesCountResponse { Count = count });
    }
}