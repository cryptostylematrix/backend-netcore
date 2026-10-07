namespace UI.Dto;

public sealed class TonConnectionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyCollection<string> Errors { get; init; } = [];
}
