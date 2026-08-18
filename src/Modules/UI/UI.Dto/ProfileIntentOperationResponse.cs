namespace UI.Dto;

public sealed class ProfileIntentOperationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyCollection<string> Errors { get; init; } = [];

    [JsonPropertyName("available_modes")]
    public IReadOnlyCollection<ProfileModeResponse> AvailableModes { get; init; } = [];
}
