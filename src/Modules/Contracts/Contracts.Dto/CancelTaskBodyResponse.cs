namespace Contracts.Dto;

public sealed class CancelTaskBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}