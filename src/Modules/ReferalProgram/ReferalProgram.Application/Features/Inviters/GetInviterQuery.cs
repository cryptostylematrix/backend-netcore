namespace ReferalProgram.Application.Features.Inviters;

public sealed record GetInviterQuery(string MarketingAddr, string ProfileAddr)
    : IQuery<GetInviterResponse>;

internal sealed class GetInviterQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetInviterQuery, GetInviterResponse>
{
    private const byte StructureNumber = 0;
    private const uint RootPlaceNumber = 1;

    public async Task<Result<GetInviterResponse>> Handle(
        GetInviterQuery request,
        CancellationToken cancellationToken)
    {
        var place = await placeQueries.GetPlaceAsync(
            marketingAddr: request.MarketingAddr,
            structureNumber: StructureNumber,
            profileAddr: request.ProfileAddr,
            placeNumber: RootPlaceNumber,
            cancellationToken);

        if (place is null)
        {
            return Result<GetInviterResponse>.NotFound();
        }

        if (place.ParentPlaceNumber is null)
        {
            return Result.Success(new GetInviterResponse
            {
                InviterProfileAddr = request.ProfileAddr
            });
        }

        var inviterPlace = await placeQueries.GetPlaceAsync(
            marketingAddr: request.MarketingAddr,
            structureNumber: StructureNumber,
            profileAddr: place.ParentProfileAddr,
            placeNumber: place.ParentPlaceNumber.Value,
            cancellationToken);

        return Result.Success(new GetInviterResponse
        {
            InviterProfileAddr = inviterPlace?.ProfileAddr
        });
    }
}
