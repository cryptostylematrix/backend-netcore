namespace ReferalProgram.Application.Features.Invites;

public sealed record GetInviteInfoQuery(string MarketingAddr, string ProfileAddr)
    : IQuery<InviteDataResponse>;

internal sealed class GetInviteInfoQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetInviteInfoQuery, InviteDataResponse>
{
    private const byte StructureNumber = 0;
    private const uint RootPlaceNumber = 1;

    public async Task<Result<InviteDataResponse>> Handle(
        GetInviteInfoQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetPlaceAsync(
            marketingAddr: request.MarketingAddr,
            structureNumber: StructureNumber,
            profileAddr: request.ProfileAddr,
            placeNumber: RootPlaceNumber,
            cancellationToken);

        if (place is null) return Result<InviteDataResponse>.NotFound();

        var structures = await placeQueries.GetProfileStructureNumbersAsync(
            request.MarketingAddr, place.ProfileAddr is { } profile ? [profile] : [], cancellationToken);
        return Result.Success(place.ToInviteData(
            place.ProfileAddr is { } address && structures.TryGetValue(address, out var numbers) ? numbers : []));
    }
}
