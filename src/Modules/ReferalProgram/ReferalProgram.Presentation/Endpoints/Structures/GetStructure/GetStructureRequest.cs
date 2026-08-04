namespace ReferalProgram.Presentation.Endpoints.Structures.GetStructure;

public sealed class GetStructureRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }
}
