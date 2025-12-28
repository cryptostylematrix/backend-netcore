namespace Contracts.Dto;

public class NftAddressResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
}