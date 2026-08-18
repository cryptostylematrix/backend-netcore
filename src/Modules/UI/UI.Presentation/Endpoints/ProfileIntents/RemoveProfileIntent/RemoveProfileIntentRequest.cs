namespace UI.Presentation.Endpoints.ProfileIntents.RemoveProfileIntent;

public sealed class RemoveProfileIntentRequest
{
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = null!;

    [BindFrom("login")]
    public string Login { get; init; } = null!;
}
