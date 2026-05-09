namespace Contracts.Dto;

public sealed class FirstTaskResponse
{
    [JsonPropertyName("key")]
    public int? Key { get; init; } 
    
    [JsonPropertyName("val")]
    public MarketingTaskResponse? Val { get; init; }
    
    [JsonPropertyName("flag")]
    public int Flag { get; init; }
}

