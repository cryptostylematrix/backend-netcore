namespace ReferalProgram.Dto;

public sealed class ProgramStatisticsResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [JsonPropertyName("referrals")]
    public ReferralCountStatisticsResponse Referrals { get; init; } = null!;

    [JsonPropertyName("structures")]
    public IReadOnlyCollection<StructureStatisticsResponse> Structures { get; init; } = [];
}

public sealed class ReferralCountStatisticsResponse
{
    [JsonPropertyName("total")]
    public long Total { get; init; }

    [JsonPropertyName("active")]
    public long Active { get; init; }

    [JsonPropertyName("inactive")]
    public long Inactive { get; init; }
}

public sealed class StructureStatisticsResponse
{
    [JsonPropertyName("structure_number")]
    public byte StructureNumber { get; init; }

    [JsonPropertyName("total_places")]
    public long TotalPlaces { get; init; }

    [JsonPropertyName("active_places")]
    public long ActivePlaces { get; init; }

    [JsonPropertyName("total_profiles")]
    public long TotalProfiles { get; init; }

    [JsonPropertyName("active_profiles")]
    public long ActiveProfiles { get; init; }

    [JsonPropertyName("referrals")]
    public StructureReferralStatisticsResponse Referrals { get; init; } = null!;
}

public sealed class StructureReferralStatisticsResponse
{
    [JsonPropertyName("total")]
    public long Total { get; init; }

    [JsonPropertyName("active")]
    public long Active { get; init; }

    [JsonPropertyName("inactive")]
    public long Inactive { get; init; }

    [JsonPropertyName("total_places")]
    public long TotalPlaces { get; init; }

    [JsonPropertyName("active_places")]
    public long ActivePlaces { get; init; }
}
