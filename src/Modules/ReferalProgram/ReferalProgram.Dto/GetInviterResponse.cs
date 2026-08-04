using System.Text.Json.Serialization;

namespace ReferalProgram.Dto;

public sealed class GetInviterResponse
{
    [JsonPropertyName("inviter_profile_addr")]
    public string? InviterProfileAddr { get; init; }
}
