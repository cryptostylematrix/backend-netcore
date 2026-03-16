namespace Contracts.Dto;

public class MarketingTransactionResponse
{
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = null!;
    
    [JsonPropertyName("lt")]
    public ulong Lt { get; init; }
    
    [JsonPropertyName("unix_time")]
    public uint UTime { get; init; }

    [JsonPropertyName("messages")]
    public MarketingTransactionMessageResponse[] Messages { get; init; } = null!;
}