namespace Contracts.Presentation.Endpoints.Marketing.BuildPayBonusBody;


public sealed class BuildPayBonusBodyRequest
{
    [BindFrom("key")]
    public uint Key { get; init; }
    
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = null!;
}