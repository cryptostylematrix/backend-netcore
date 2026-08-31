using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Mappings;

internal static class PlaceResponseMapper
{
    public static PlaceResponse Map(Place place) => new()
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
