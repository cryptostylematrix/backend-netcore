namespace Marketing.Application.Features.Places;

public sealed record GetPlacesQuery(string MarketingAddr, byte M, string ProfileAddr, int Page, int PageSize)
    : IQuery<Paginated<PlaceResponse>>;

internal sealed class GetPlacesQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPlacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(
        GetPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await placeQueries.GetPlacesAsync(
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}