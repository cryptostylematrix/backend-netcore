using Common.Domain;

namespace UI.Core.WalletProfileIntentAggregate.Events;

public sealed record WalletProfileIntentRemovedDomainEvent(
    Guid EventId,
    DateTime RemovedAtUtc,
    string WalletAddr,
    string ProfileAddr,
    WalletProfileMode Mode,
    bool Owned)
    : DomainEvent(EventId, RemovedAtUtc);
