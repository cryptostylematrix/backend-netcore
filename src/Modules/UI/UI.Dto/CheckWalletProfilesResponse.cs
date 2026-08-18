namespace UI.Dto;

public sealed class CheckWalletProfilesResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyCollection<string> Errors { get; init; } = [];

    [JsonPropertyName("profiles")]
    public IReadOnlyCollection<WalletProfileResponse> Profiles { get; init; } = [];
}
