namespace ReferalProgram.Dto;

public sealed class ProfileVolumeResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [JsonPropertyName("structure_number")]
    public byte StructureNumber { get; init; }

    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [JsonPropertyName("personal_volume")]
    public uint PersonalVolume { get; init; }

    [JsonPropertyName("referral_volume")]
    public uint ReferralVolume { get; init; }

    [JsonPropertyName("group_volume")]
    public uint GroupVolume { get; init; }
}
