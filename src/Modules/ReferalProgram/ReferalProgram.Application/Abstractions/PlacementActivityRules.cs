namespace ReferalProgram.Application.Abstractions;

public sealed record PlacementActivityRules(
    string? ChildProfileAddr,
    string? InviterProfileAddr,
    bool AllowOwnChildren,
    bool AllowInactiveSpillover,
    bool RequireActiveInvite,
    bool CheckManualPlacement)
{
    public bool ChangesAutomaticEligibility => AllowOwnChildren || AllowInactiveSpillover || RequireActiveInvite;

    public bool IsOwnChild(string? parentProfileAddr) => parentProfileAddr is not null
        && ChildProfileAddr is not null
        && (parentProfileAddr == ChildProfileAddr || parentProfileAddr == InviterProfileAddr);

    public bool Allows(PlaceResponse parent, bool inviteActive, bool manual = false)
    {
        if (manual && !CheckManualPlacement)
            return true;
        var own = IsOwnChild(parent.ProfileAddr);
        return (parent.IsActive || (own ? AllowOwnChildren : AllowInactiveSpillover))
            && (own || !RequireActiveInvite || parent.ProfileAddr is null || inviteActive);
    }
}
