namespace Contracts.Presentation.Endpoints.JettonWallet.GetWalletData;

public sealed class GetWalletDataRequest
{
    public string Addr { get; init; } = null!;
}