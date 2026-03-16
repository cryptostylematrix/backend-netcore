namespace Contracts.Dto;

public sealed class WalletTransactionHistoryResponse
{
    [JsonPropertyName("items")]
    public WalletTransactionResponse[] Items { get; init; } = [];
}