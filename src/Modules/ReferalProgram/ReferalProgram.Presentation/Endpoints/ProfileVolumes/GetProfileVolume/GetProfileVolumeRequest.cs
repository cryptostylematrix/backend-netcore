namespace ReferalProgram.Presentation.Endpoints.ProfileVolumes.GetProfileVolume;

public sealed class GetProfileVolumeRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
