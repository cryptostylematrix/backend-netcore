namespace ReferalProgram.Application.Features.Places;

public sealed record SearchPlacesQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    string Query,
    int Page,
    int PageSize) : IQuery<Paginated<PlaceResponse>>;

internal sealed class SearchPlacesQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<SearchPlacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(
        SearchPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var places = await placeQueries.SearchPlacesAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            request.Query.Trim(),
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(places);
    }
}
