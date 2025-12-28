namespace Contracts.Dto;

public sealed class ContractBalanceResponse
{
    [JsonPropertyName("balance")]
    public decimal Balance { get; init; }
}