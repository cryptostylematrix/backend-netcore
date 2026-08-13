namespace ReferalProgram.Presentation.Endpoints.Places.GetPath;

public sealed class GetPathRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("viewer_profile_addr")]
    public string ViewerProfileAddr { get; init; } = null!;

    [BindFrom("target_profile_addr")]
    public string? TargetProfileAddr { get; init; }

    [BindFrom("target_place_number")]
    public uint TargetPlaceNumber { get; init; }
}
