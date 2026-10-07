namespace ReferalProgram.Application.Features.Invites;

public sealed record GetReferralsQuery(
    string MarketingAddr,
    string ProfileAddr,
    int Page,
    int PageSize) : IQuery<Paginated<InviteDataResponse>>;

internal sealed class GetReferralsQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetReferralsQuery, Paginated<InviteDataResponse>>
{
    private const byte StructureNumber = 0;
    private const uint PlaceNumber = 1;

    public async Task<Result<Paginated<InviteDataResponse>>> Handle(
        GetReferralsQuery request,
        CancellationToken cancellationToken)
    {
        var places = await placeQueries.GetChildrenAsync(
            marketingAddr: request.MarketingAddr,
            structureNumber: StructureNumber,
            parentProfileAddr: request.ProfileAddr,
            parentPlaceNumber: PlaceNumber,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken);

        var items = places.Items.ToArray();
        var structures = await placeQueries.GetProfileStructureNumbersAsync(
            request.MarketingAddr,
            items.Select(place => place.ProfileAddr).OfType<string>().Distinct().ToArray(),
            cancellationToken);

        return Result.Success(new Paginated<InviteDataResponse>
        {
            Items = items.Select(place => place.ToInviteData(
                place.ProfileAddr is { } address && structures.TryGetValue(address, out var numbers) ? numbers : [])),
            Page = places.Page,
            TotalPages = places.TotalPages
        });
    }
}
