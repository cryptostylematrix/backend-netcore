namespace Contracts.Dto;

public sealed class TransactionMessageResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; }  = null!;
    
    [JsonPropertyName("value")]
    public decimal Value {get; init;}
    
    [JsonPropertyName("op")]
    public string Op { get; init; } = null!;
    
    [JsonPropertyName("comment")]
    public string Comment { get; init; } = null!;

    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}