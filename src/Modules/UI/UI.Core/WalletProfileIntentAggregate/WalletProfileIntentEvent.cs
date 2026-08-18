namespace UI.Core.WalletProfileIntentAggregate;

public sealed class WalletProfileIntentEvent
{
    private WalletProfileIntentEvent()
    {
    }

    private WalletProfileIntentEvent(
        Guid eventId,
        string walletAddr,
        string profileAddr,
        WalletProfileIntentEventType eventType,
        string dataJson,
        DateTime occurredAtUtc)
    {
        EventId = eventId;
        WalletAddr = walletAddr;
        ProfileAddr = profileAddr;
        EventType = eventType;
        DataJson = dataJson;
        OccurredAtUtc = occurredAtUtc;
    }

    public long Id { get; private set; }
    public Guid EventId { get; private set; }
    public string WalletAddr { get; private set; } = null!;
    public string ProfileAddr { get; private set; } = null!;
    public WalletProfileIntentEventType EventType { get; private set; }
    public string DataJson { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }

    public static WalletProfileIntentEvent Create(
        Guid eventId,
        string walletAddr,
        string profileAddr,
        WalletProfileIntentEventType eventType,
        string dataJson,
        DateTime occurredAtUtc) =>
        new(
            eventId,
            walletAddr,
            profileAddr,
            eventType,
            dataJson,
            occurredAtUtc.Kind == DateTimeKind.Utc
                ? occurredAtUtc
                : occurredAtUtc.ToUniversalTime());
}
