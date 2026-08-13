namespace ReferalProgram.Presentation.Endpoints.Places.GetPurchaseOption;

public sealed class GetPurchaseOptionRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [BindFrom("parent_profile_addr")]
    public string? ParentProfileAddr { get; init; }

    [BindFrom("parent_place_number")]
    public uint? ParentPlaceNumber { get; init; }

    [BindFrom("position")]
    public uint? Position { get; init; }
}
