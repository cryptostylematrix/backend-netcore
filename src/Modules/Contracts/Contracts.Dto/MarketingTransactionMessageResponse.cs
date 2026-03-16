namespace Contracts.Dto;

public class MarketingTransactionMessageResponse
{
    [JsonPropertyName("value")]
    public decimal Value {get; init;}
    
    [JsonPropertyName("op")]
    public string Op { get; init; } = null!;
    
    [JsonPropertyName("comment")]
    public string Comment { get; init; } = null!;

    [JsonPropertyName("from_addr")]
    public string FromAddr { get; init; } = null!;
    
    [JsonPropertyName("to_addr")]
    public string ToAddr { get; init; } = null!;
    
    [JsonPropertyName("query_id")]
    public ulong QueryId { get; init; }
    
    [JsonPropertyName("key")]
    public ulong Key { get; init; }
    
    [JsonPropertyName("m")]
    public ulong M { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [JsonPropertyName("pos")]
    public ulong Pos { get; init; }
}