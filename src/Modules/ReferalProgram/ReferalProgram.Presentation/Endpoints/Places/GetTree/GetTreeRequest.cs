namespace ReferalProgram.Presentation.Endpoints.Places.GetTree;

public sealed class GetTreeRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("profile_addr")]
    public string? ProfileAddr { get; init; }

    [BindFrom("place_number")]
    public uint PlaceNumber { get; init; }

    [BindFrom("from_pos")]
    public uint FromPos { get; init; }

    [BindFrom("to_pos")]
    public uint ToPos { get; init; }
}
