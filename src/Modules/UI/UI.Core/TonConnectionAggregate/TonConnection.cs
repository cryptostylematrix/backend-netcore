using Common.Domain;

namespace UI.Core.TonConnectionAggregate;

// Latest client-reported connection metadata for one canonical wallet address.
public sealed class TonConnection : Entity, IAggregateRoot
{
    private TonConnection() { }

    public string WalletAddr { get; private set; } = null!;
    public string ContractVersion { get; private set; } = null!;
    public string WalletName { get; private set; } = null!;
    public string AppVersion { get; private set; } = null!;
    public string Platform { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastConnectedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static TonConnection Create(string walletAddr, string contractVersion,
        string walletName, string appVersion, string platform, DateTime nowUtc) => new()
    {
        WalletAddr = walletAddr,
        ContractVersion = contractVersion,
        WalletName = walletName,
        AppVersion = appVersion,
        Platform = platform,
        CreatedAtUtc = nowUtc,
        LastConnectedAtUtc = nowUtc,
        UpdatedAtUtc = nowUtc
    };
}
