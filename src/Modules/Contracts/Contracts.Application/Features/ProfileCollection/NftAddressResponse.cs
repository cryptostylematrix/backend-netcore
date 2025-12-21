namespace Contracts.Application.Features.ProfileCollection;

public class NftAddressResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
}