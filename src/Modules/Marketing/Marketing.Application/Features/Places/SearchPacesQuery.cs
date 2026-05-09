namespace Marketing.Application.Features.Places;

public sealed record SearchPacesQuery(
    string MarketingAddr,
    byte M,
    string ProfileAddr,
    int Page,
    int PageSize,
    string Query)
    : IQuery<Paginated<PlaceResponse>>;

internal sealed class SearchPacesQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<SearchPacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(
        SearchPacesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await placeQueries.SearchPlacesAsync(
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr,
            query: request.Query,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}