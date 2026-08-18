namespace UI.Presentation.Endpoints.ProfileIntents.CheckWalletProfiles;

public sealed class CheckWalletProfilesRequest
{
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = null!;
}
