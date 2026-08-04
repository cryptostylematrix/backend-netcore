using System.Text.Json.Serialization;

namespace ReferalProgram.Dto;

public sealed class InviteDataResponse
{
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [JsonPropertyName("profile_login")]
    public string ProfileLogin { get; init; } = null!;

    [JsonPropertyName("inviter_profile_addr")]
    public string? InviterProfileAddr { get; init; }

    [JsonPropertyName("inviter_profile_login")]
    public string? InviterProfileLogin { get; init; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }

    [JsonPropertyName("activated_at")]
    public long? ActivatedAt { get; init; }

    [JsonPropertyName("filling")]
    public uint Filling { get; init; }

    [JsonPropertyName("ative")]
    public bool IsActive { get; init; }
}
