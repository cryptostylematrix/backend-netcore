namespace UI.Presentation.Endpoints.ProfileIntents.GetWalletProfiles;

public sealed class GetWalletProfilesRequest
{
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = null!;
}
