using Common.Domain;

namespace UI.Core.WalletProfileIntentAggregate.Events;

public sealed record WalletProfileOwnershipLostDomainEvent(
    Guid EventId,
    DateTime LostAtUtc,
    string WalletAddr,
    string ProfileAddr,
    WalletProfileMode Mode)
    : DomainEvent(EventId, LostAtUtc);
