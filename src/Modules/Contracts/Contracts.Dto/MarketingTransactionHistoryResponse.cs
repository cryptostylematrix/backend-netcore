namespace Contracts.Dto;

public class MarketingTransactionHistoryResponse
{
    [JsonPropertyName("items")]
    public MarketingTransactionResponse[] Items { get; init; } = [];
}