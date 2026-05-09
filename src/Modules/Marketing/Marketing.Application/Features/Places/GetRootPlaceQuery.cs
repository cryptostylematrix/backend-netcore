namespace Marketing.Application.Features.Places;

public sealed record GetRootPlaceQuery(string MarketingAddr, byte M, string ProfileAddr) : IQuery<PlaceResponse>;

internal sealed class GetRootPlaceQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetRootPlaceQuery, PlaceResponse>
{
    public async Task<Result<PlaceResponse>> Handle(GetRootPlaceQuery request, CancellationToken cancellationToken)
    {
        var root = await placeQueries.GetRootPlaceAsync(
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr, 
            cancellationToken);

        return root is null
            ? Result<PlaceResponse>.NotFound()
            : Result.Success(root);
    }
}