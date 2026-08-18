using Common.Domain;

namespace UI.Core.WalletProfileIntentAggregate.Events;

public sealed record WalletProfileOwnershipGainedDomainEvent(
    Guid EventId,
    DateTime GainedAtUtc,
    string WalletAddr,
    string ProfileAddr,
    WalletProfileMode Mode)
    : DomainEvent(EventId, GainedAtUtc);
