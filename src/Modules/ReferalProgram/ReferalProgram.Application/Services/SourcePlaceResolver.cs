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
        var sourcePlace = await placeRepository.GetAncestorAsync(
            place,
            structureHeight,
            cancellationToken);

        if (sourcePlace is null)
            return null;

        var placesCount = await placeRepository.CountAtDepthAsync(
            place.MarketingAddr,
            place.StructureNumber,
            sourcePlace.Mp,
            place.Deep,
            cancellationToken);

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
