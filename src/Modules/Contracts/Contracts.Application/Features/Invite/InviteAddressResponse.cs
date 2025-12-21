namespace Contracts.Application.Features.Invite;

public sealed class InviteAddressResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
}