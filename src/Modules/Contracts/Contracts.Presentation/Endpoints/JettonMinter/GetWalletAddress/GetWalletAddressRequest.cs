namespace Contracts.Presentation.Endpoints.JettonMinter.GetWalletAddress;

public sealed class GetWalletAddressRequest
{
    public string Addr { get; init; } = null!;
    public string OwnerAddr { get; init; } = null!;
}