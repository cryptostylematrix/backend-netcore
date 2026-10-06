namespace ReferalProgram.Presentation.Endpoints.Places.GetActivationOption;

public sealed class GetActivationOptionRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    [BindFrom("place_number")]
    public uint PlaceNumber { get; init; }
}
