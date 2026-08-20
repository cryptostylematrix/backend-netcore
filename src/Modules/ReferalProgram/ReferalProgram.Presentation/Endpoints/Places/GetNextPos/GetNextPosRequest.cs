namespace ReferalProgram.Presentation.Endpoints.Places.GetNextPos;

public sealed class GetNextPosRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [BindFrom("operation")]
    public string? Operation { get; init; }
}
