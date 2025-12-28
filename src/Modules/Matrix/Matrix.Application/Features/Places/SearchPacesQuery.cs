namespace Matrix.Application.Features.Places;

public sealed record SearchPacesQuery(
    short M,
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
            request.M,
            request.ProfileAddr,
            request.Query,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}