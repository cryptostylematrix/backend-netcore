namespace ReferalProgram.Application.Features.Places;

public sealed record ResolveBonusQuery(
    string MarketingAddr,
    uint BonusTypeTag,
    byte StructureNumber,
    string? RelativeProfileAddr,
    uint RelativePlaceNumber,
    ushort Level) : IQuery<BonusResponse>;

internal sealed class ResolveBonusQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<ResolveBonusQuery, BonusResponse>
{
    private const uint RefBonusTag = 0xb5ce6bf5;
    private const uint StructBonusTag = 0xe1319040;
    private const uint DevBonusTag = 0x1b5547d5;

    public async Task<Result<BonusResponse>> Handle(
        ResolveBonusQuery request,
        CancellationToken cancellationToken)
    {
        var sourcePlace = await placeQueries.GetPlaceAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.RelativeProfileAddr,
            request.RelativePlaceNumber,
            cancellationToken);

        if (sourcePlace is null)
            return Result<BonusResponse>.Error("The source place was not found.");

        var relativePlace = await FindEligiblePlaceAsync(
            sourcePlace,
            request.Level,
            cancellationToken);

        if (relativePlace is null)
            return Result<BonusResponse>.Error("An eligible relative place was not found.");

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

                recipientPlace = await FindEligiblePlaceAsync(
                    invite,
                    level: 1,
                    cancellationToken);
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

    /* 
        level cannot be a starting ppint becasue in this casse we can 
        take the same place twice
    */

    private async Task<PlaceResponse?> FindEligiblePlaceAsync(
        PlaceResponse? start,
        ushort level,
        CancellationToken cancellationToken)
    {
        var current = start;
        var eligibleLevel = 0;

        while (current is not null)
        {
            var isEligible = current.IsActive
                && !string.IsNullOrWhiteSpace(current.ProfileAddr);

            if (isEligible)
            {
                if (eligibleLevel == level)
                    return current;

                eligibleLevel++;
            }

            if (current.ParentId is null)
                return isEligible ? current : null;

            current = await placeQueries.GetPlaceAsync(
                current.ParentId.Value,
                cancellationToken);
        }

        return null;
    }
}
