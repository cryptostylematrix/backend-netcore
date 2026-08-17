namespace ReferalProgram.Presentation.Endpoints.StructureRanks.GetStructureRanks;

public sealed class GetStructureRanksRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }
}
