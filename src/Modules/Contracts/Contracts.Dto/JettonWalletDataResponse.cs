namespace Contracts.Dto;

public sealed class JettonWalletDataResponse
{
    [JsonPropertyName("balance")]
    public ulong Balance { get; init; }
    
    [JsonPropertyName("owner_addr")]
    public string OwnerAddr { get; init; } = null!;

    [JsonPropertyName("minter_addr")]
    public string MinterAddr { get; init; } = null!;
    
    [JsonIgnore]
    public string WalletCode { get; init; } = null!;
}