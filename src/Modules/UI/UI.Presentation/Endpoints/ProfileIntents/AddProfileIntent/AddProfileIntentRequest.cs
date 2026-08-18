namespace UI.Presentation.Endpoints.ProfileIntents.AddProfileIntent;

public sealed class AddProfileIntentRequest
{
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = null!;

    [BindFrom("login")]
    public string Login { get; init; } = null!;

    [BindFrom("mode")]
    public ProfileModeResponse? Mode { get; init; }
}
