namespace Contracts.Dto;

public sealed class BuyPlaceByTonBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}