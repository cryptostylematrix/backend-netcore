namespace ReferalProgram.Application.Services;

public sealed class RelativePlaceResolver(IPlaceQueries placeQueries)
    : IRelativePlaceResolver
{
    public async Task<RelativePlaceResolution?> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        ushort level,
        CancellationToken cancellationToken)
    {
        var sourcePlace = await placeQueries.GetPlaceAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            placeNumber,
            cancellationToken);

        if (sourcePlace is null)
            return null;

        var relativePlace = await FindEligiblePlaceAsync(
            sourcePlace,
            level,
            cancellationToken);

        return relativePlace is null
            ? null
            : new RelativePlaceResolution(sourcePlace, relativePlace);
    }

    private async Task<PlaceResponse?> FindEligiblePlaceAsync(
        PlaceResponse start,
        ushort level,
        CancellationToken cancellationToken)
    {
        PlaceResponse? current = start;
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
