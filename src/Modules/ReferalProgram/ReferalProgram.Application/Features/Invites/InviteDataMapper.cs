namespace ReferalProgram.Application.Features.Invites;

internal static class InviteDataMapper
{
    public static InviteDataResponse ToInviteData(this PlaceResponse place) => new()
    {
        ProfileAddr = place.ProfileAddr!,
        ProfileLogin = place.ProfileLogin ?? string.Empty,
        InviterProfileAddr = place.ParentProfileAddr,
        InviterProfileLogin = place.ParentProfileLogin,
        CreatedAt = place.CreatedAt,
        ActivatedAt = place.ActivatedAt,
        Filling = place.Filling,
        IsActive = place.IsActive
    };
}
