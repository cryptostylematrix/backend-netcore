namespace Contracts.Dto;

public sealed class UnlockPosBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}