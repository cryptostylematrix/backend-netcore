namespace Contracts.Dto;

public sealed class TransactionResponse
{
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = null!;
    
    [JsonPropertyName("lt")]
    public ulong Lt { get; init; }
    
    [JsonPropertyName("unix_time")]
    public uint UTime { get; init; }

    [JsonPropertyName("messages")]
    public TransactionMessageResponse[] Messages { get; init; } = null!;
}