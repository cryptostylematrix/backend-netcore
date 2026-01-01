namespace Contracts.Dto;

public sealed class BuyPlaceBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}