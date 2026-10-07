namespace ReferalProgram.Application.Features.Invites;

public sealed record GetRootInviteInfoQuery(string MarketingAddr)
    : IQuery<InviteDataResponse>;

internal sealed class GetRootInviteInfoQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetRootInviteInfoQuery, InviteDataResponse>
{
    private const byte StructureNumber = 0;

    public async Task<Result<InviteDataResponse>> Handle(
        GetRootInviteInfoQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetRootPlaceAsync(
            marketingAddr: request.MarketingAddr,
            structureNumber: StructureNumber,
            cancellationToken);

        if (place is null) return Result<InviteDataResponse>.NotFound();

        var structures = await placeQueries.GetProfileStructureNumbersAsync(
            request.MarketingAddr, place.ProfileAddr is { } profile ? [profile] : [], cancellationToken);
        return Result.Success(place.ToInviteData(
            place.ProfileAddr is { } address && structures.TryGetValue(address, out var numbers) ? numbers : []));
    }
}
