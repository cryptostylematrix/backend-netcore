namespace Contracts.Dto;

public sealed class InviteAddressResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
}