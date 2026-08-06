namespace ReferalProgram.Presentation.Endpoints.Places.GetTopPlace;

public sealed class GetTopPlaceRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }
}
