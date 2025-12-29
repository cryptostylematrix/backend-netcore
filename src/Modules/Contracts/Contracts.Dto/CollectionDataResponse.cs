namespace Contracts.Dto;

public sealed class CollectionDataResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
    
    [JsonPropertyName("owner_addr")] 
    public string OwnerAddr { get; init; } = null!;
}