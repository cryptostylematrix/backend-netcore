namespace Contracts.Dto;

public sealed class WalletTransactionResponse
{
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = null!;
    
    [JsonPropertyName("lt")]
    public ulong Lt { get; init; }
    
    [JsonPropertyName("unix_time")]
    public uint UTime { get; init; }

    [JsonPropertyName("messages")]
    public WalletTransactionMessageResponse[] Messages { get; init; } = null!;
}