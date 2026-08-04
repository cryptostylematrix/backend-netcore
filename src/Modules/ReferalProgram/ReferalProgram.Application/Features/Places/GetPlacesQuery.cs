namespace ReferalProgram.Application.Features.Places;

public sealed record GetPlacesQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    int Page,
    int PageSize) : IQuery<Paginated<PlaceResponse>>;

internal sealed class GetPlacesQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPlacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(
        GetPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var places = await placeQueries.GetPlacesAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(places);
    }
}
