using Common.Domain;

namespace UI.Core.WalletPreferencesAggregate;

public sealed class WalletPreferences : Entity, IAggregateRoot
{
    private WalletPreferences() { }
    public string WalletAddr { get; private set; } = null!;
    public string Language { get; private set; } = null!;
    public DateTime UpdatedAtUtc { get; private set; }

    public static WalletPreferences Create(string walletAddr, string language, DateTime nowUtc) => new()
    {
        WalletAddr = walletAddr,
        Language = language,
        UpdatedAtUtc = nowUtc
    };
}
