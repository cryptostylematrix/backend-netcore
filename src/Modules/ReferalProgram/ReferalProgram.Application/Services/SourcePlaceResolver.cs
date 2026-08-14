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
                SourcePlace: ToResponse(sourcePlace));
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
            SourcePlace: ToResponse(sourcePlace));
    }

    private static PlaceResponse ToResponse(Place place) => new()
    {
        Id = place.Id,
        ParentId = place.ParentId,
        Mp = place.Mp,
        PosGroup = place.PosGroup,
        MarketingAddr = place.MarketingAddr,
        StructNumber = place.StructureNumber,
        ProfileAddr = place.ProfileAddr,
        PlaceNumber = place.PlaceNumber,
        ProfileLogin = place.ProfileLogin,
        Index = place.Index,
        ParentProfileAddr = place.ParentProfileAddr,
        ParentProfileLogin = place.ParentProfileLogin,
        ParentPlaceNumber = place.ParentPlaceNumber,
        CreatedAt = place.CreatedAt,
        ActivatedAt = place.ActivatedAt,
        IsActive = place.IsActive,
        Kind = place.Kind,
        Pos = place.Pos,
        Filling = place.Filling,
        Deep = place.Deep,
        PersonalVolume = place.PersonalVolume,
        GroupVolume = place.GroupVolume
    };
}
