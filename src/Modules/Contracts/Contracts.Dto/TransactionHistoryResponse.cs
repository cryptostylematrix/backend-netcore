namespace Contracts.Dto;

public sealed class TransactionHistoryResponse
{
    [JsonPropertyName("items")]
    public TransactionResponse[] Items { get; init; } = [];
}