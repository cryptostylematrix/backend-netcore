using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Services;

public sealed class SourcePlaceResolver(IPlaceRepository placeRepository)
    : ISourcePlaceResolver
{
    public async Task<SourcePlaceResolution?> ResolveAsync(
        Place place,
        byte structureHeight,
        CancellationToken cancellationToken)
    {
        var sourcePlace = place;
        byte reachedHeight = 0;
        while (reachedHeight < structureHeight)
        {
            if (sourcePlace.ParentId is null)
                break;

            var parent = await placeRepository.GetByIdAsync(
                sourcePlace.ParentId.Value,
                cancellationToken);
            if (parent is null)
                break;

            sourcePlace = parent;
            reachedHeight++;
        }

        if (reachedHeight < structureHeight)
        {
            return new SourcePlaceResolution(
                Code: 0,
                SourcePlace: sourcePlace);
        }

        var placesCount = await placeRepository.CountAtDepthAsync(
            place.MarketingAddr,
            place.StructureNumber,
            sourcePlace.Mp,
            place.Deep,
            cancellationToken);

        // Resolution happens before SaveChanges so a cancelled command cannot
        // persist its place. The database count therefore does not yet contain
        // the pending place, although it belongs to the source's depth slice.
        if (place.Id == 0)
            placesCount = checked(placesCount + 1);

        return new SourcePlaceResolution(
            Code: checked((uint)placesCount),
            SourcePlace: sourcePlace);
    }
}
