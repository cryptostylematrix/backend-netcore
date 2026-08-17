namespace ReferalProgram.Dto;

public sealed class StructureRankResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [JsonPropertyName("structure_number")]
    public byte StructureNumber { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("required_active_referral_places")]
    public uint RequiredActiveReferralPlaces { get; init; }
}
