namespace ReferalProgram.Presentation.Endpoints.Places.GetPath;

public sealed class GetPathRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("from_profile_addr")]
    public string? FromProfileAddr { get; init; }

    [BindFrom("from_place_number")]
    public uint FromPlaceNumber { get; init; }

    [BindFrom("to_profile_addr")]
    public string? ToProfileAddr { get; init; }

    [BindFrom("to_place_number")]
    public uint ToPlaceNumber { get; init; }
}
