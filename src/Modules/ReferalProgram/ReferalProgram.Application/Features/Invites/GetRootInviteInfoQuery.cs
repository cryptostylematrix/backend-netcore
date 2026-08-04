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

        return place is null
            ? Result<InviteDataResponse>.NotFound()
            : Result.Success(place.ToInviteData());
    }
}
