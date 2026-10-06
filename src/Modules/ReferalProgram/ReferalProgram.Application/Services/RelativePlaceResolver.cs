namespace ReferalProgram.Application.Services;

public sealed class RelativePlaceResolver(IPlaceQueries placeQueries, IStructureQueries structureQueries)
    : IRelativePlaceResolver
{
    public async Task<RelativePlaceResolution?> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        ushort level,
        CancellationToken cancellationToken,
        RecipientPurpose purpose = RecipientPurpose.Bonus)
    {
        var sourcePlace = await placeQueries.GetPlaceAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            placeNumber,
            cancellationToken);

        if (sourcePlace is null)
            return null;

        var structure = await structureQueries.GetStructureAsync(marketingAddr, structureNumber, cancellationToken);
        var rules = ActivitySettings.ParseRecipientRules(structure?.Activity, structureNumber);
        var allowInactive = purpose switch
        {
            RecipientPurpose.Bonus => rules?.AllowAsBonusRecipient == true,
            RecipientPurpose.Clone => rules?.AllowAsCloneRecipient == true,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose))
        };
        var relativePlace = await FindEligiblePlaceAsync(
            sourcePlace,
            level,
            allowInactive,
            cancellationToken);

        return relativePlace is null
            ? null
            : new RelativePlaceResolution(sourcePlace, relativePlace);
    }

    private async Task<PlaceResponse?> FindEligiblePlaceAsync(
        PlaceResponse start,
        ushort level,
        bool allowInactive,
        CancellationToken cancellationToken)
    {
        PlaceResponse? current = start;
        var eligibleLevel = 0;

        while (current is not null)
        {
            var isEligible = (current.IsActive || allowInactive)
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
