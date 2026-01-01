namespace Contracts.Dto;

public sealed class LockPosBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}