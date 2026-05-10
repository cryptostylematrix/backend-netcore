namespace Contracts.Dto;

public sealed class PlaceAddressResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
}