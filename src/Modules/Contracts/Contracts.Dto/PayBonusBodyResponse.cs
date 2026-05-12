namespace Contracts.Dto;

public sealed class PayBonusBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}