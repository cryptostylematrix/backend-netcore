namespace UI.Dto;

public sealed class WalletLanguageResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
    [JsonPropertyName("language")]
    public string? Language { get; init; }
    [JsonPropertyName("errors")]
    public IReadOnlyCollection<string> Errors { get; init; } = [];
}
