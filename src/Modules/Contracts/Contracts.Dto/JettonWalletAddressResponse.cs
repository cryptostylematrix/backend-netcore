namespace Contracts.Dto;

public sealed class JettonWalletAddressResponse
{
    [JsonPropertyName("wallet_addr")]
    public string WalletAddr { get; init; } = null!;
}