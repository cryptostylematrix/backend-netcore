namespace Contracts.Presentation.Endpoints.Marketing.BuildDeployPlaceBody;


public sealed class BuildDeployPlaceBodyRequest
{
    [BindFrom("key")]
    public uint Key { get; init; }
    
    [BindFrom("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [BindFrom("kind")]
    public byte Kind { get; init; }
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [BindFrom("place_no")]
    public uint PlaceNumber { get; init; }
    
    [BindFrom("inviter_profile_addr")]
    public string? InviterProfileAddr { get; init; }
}