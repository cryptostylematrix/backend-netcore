namespace Matrix.Application.Features.Places;

public sealed record GetPlacesQuery(short M, string ProfileAddr, int Page, int PageSize)
    : IQuery<Paginated<PlaceResponse>>;

internal sealed class GetPlacesQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPlacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(
        GetPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await placeQueries.GetPlacesAsync(
            request.M,
            request.ProfileAddr,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}