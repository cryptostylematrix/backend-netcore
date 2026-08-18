using Common.Domain;

namespace UI.Core.WalletProfileIntentAggregate.Events;

public sealed record WalletProfileIntentAddedDomainEvent(
    Guid EventId,
    DateTime AddedAtUtc,
    string WalletAddr,
    string ProfileAddr,
    WalletProfileMode Mode,
    bool Owned)
    : DomainEvent(EventId, AddedAtUtc);
