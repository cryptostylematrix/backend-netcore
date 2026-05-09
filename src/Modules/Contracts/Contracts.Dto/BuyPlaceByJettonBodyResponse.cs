namespace Contracts.Dto;

public sealed class BuyPlaceByJettonBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}