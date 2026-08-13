namespace ReferalProgram.Application.Features.Places;

public sealed record ResolveBonusQuery(
    string MarketingAddr,
    uint BonusTypeTag,
    byte StructureNumber,
    string? RelativeProfileAddr,
    uint RelativePlaceNumber,
    ushort Level) : IQuery<BonusResponse>;

internal sealed class ResolveBonusQueryHandler(
    IPlaceQueries placeQueries,
    IRelativePlaceResolver relativePlaceResolver)
    : IQueryHandler<ResolveBonusQuery, BonusResponse>
{
    private const uint RefBonusTag = 0xb5ce6bf5;
    private const uint StructBonusTag = 0xe1319040;
    private const uint DevBonusTag = 0x1b5547d5;

    public async Task<Result<BonusResponse>> Handle(
        ResolveBonusQuery request,
        CancellationToken cancellationToken)
    {
        var resolution = await relativePlaceResolver.ResolveAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.RelativeProfileAddr,
            request.RelativePlaceNumber,
            request.Level,
            cancellationToken);

        if (resolution is null)
            return Result<BonusResponse>.Error("An eligible relative place was not found.");

        var sourcePlace = resolution.SourcePlace;
        var relativePlace = resolution.RelativePlace;

        PlaceResponse? recipientPlace;

        switch (request.BonusTypeTag)
        {
            case RefBonusTag:
                var invite = await placeQueries.GetPlaceAsync(
                    request.MarketingAddr,
                    structureNumber: 0,
                    relativePlace.ProfileAddr,
                    placeNumber: 1,
                    cancellationToken);

                if (invite is null)
                {
                    recipientPlace = null;
                    break;
                }

                var inviterResolution = await relativePlaceResolver.ResolveAsync(
                    invite.MarketingAddr,
                    invite.StructNumber,
                    invite.ProfileAddr,
                    invite.PlaceNumber,
                    level: 1,
                    cancellationToken);
                recipientPlace = inviterResolution?.RelativePlace;
                break;

            case StructBonusTag:
            case DevBonusTag:
                recipientPlace = relativePlace;
                break;

            default:
                return Result<BonusResponse>.Error(
                    $"Unknown bonus type tag 0x{request.BonusTypeTag:x8}.");
        }

        if (recipientPlace is null)
            return Result<BonusResponse>.Error("An active inviter was not found.");

        if (string.IsNullOrWhiteSpace(recipientPlace.ProfileAddr))
            return Result<BonusResponse>.Error("The bonus recipient has no profile address.");

        return Result.Success(new BonusResponse(
            Reason: sourcePlace,
            RecipientProfileAddr: recipientPlace.ProfileAddr));
    }
}
