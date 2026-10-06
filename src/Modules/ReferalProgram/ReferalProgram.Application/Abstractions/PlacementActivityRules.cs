namespace ReferalProgram.Application.Abstractions;

public sealed record PlacementActivityRules(
    string? ChildProfileAddr,
    string? InviterProfileAddr,
    bool AllowOwnChildren,
    bool AllowInactiveSpillover,
    bool CheckManualPlacement,
    byte? ActivityStructureNumber = null)
{
    public bool ChangesAutomaticEligibility => AllowOwnChildren || AllowInactiveSpillover || ActivityStructureNumber is not null;

    public bool RequiresProfileActivity => ActivityStructureNumber is not null;

    public bool IsOwnChild(string? parentProfileAddr) => parentProfileAddr is not null
        && ChildProfileAddr is not null
        && (parentProfileAddr == ChildProfileAddr || parentProfileAddr == InviterProfileAddr);

    public bool Allows(PlaceResponse parent, bool inviteActive, bool manual = false)
    {
        if (manual && !CheckManualPlacement)
            return true;
        var own = IsOwnChild(parent.ProfileAddr);
        var active = ActivityStructureNumber is not null && parent.ProfileAddr is not null ? inviteActive : parent.IsActive;
        return active || (own ? AllowOwnChildren : AllowInactiveSpillover);
    }
}
