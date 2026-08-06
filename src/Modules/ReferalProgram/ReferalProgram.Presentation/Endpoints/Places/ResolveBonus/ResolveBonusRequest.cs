namespace ReferalProgram.Presentation.Endpoints.Places.ResolveBonus;

public sealed class ResolveBonusRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("bonus_type_tag")]
    public uint BonusTypeTag { get; init; }

    [BindFrom("relative_profile_addr")]
    public string? RelativeProfileAddr { get; init; }

    [BindFrom("relative_place_number")]
    public uint RelativePlaceNumber { get; init; }

    [BindFrom("level")]
    public ushort Level { get; init; }
}
