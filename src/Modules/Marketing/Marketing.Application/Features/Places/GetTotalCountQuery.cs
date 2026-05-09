namespace Marketing.Application.Features.Places;


public sealed record GetTotalCountQuery(string MarketingAddr, string ProfileAddr) : IQuery<PlacesTotalCountResponse>;

internal sealed class GetTotalCountQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetTotalCountQuery, PlacesTotalCountResponse>
{
    public async Task<Result<PlacesTotalCountResponse>> Handle(GetTotalCountQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await placeQueries.GetPlacesTotalCountAsync(
            marketingAddr: request.MarketingAddr,
            profileAddr: request.ProfileAddr,
            cancellationToken: cancellationToken);

        return Result.Success(new PlacesTotalCountResponse { TotalCount = totalCount });
    }
}