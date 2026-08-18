using Common.Domain;

namespace UI.Core.ProfileAggregate.Events;

public sealed record ProfileContentChangedDomainEvent(
    Guid EventId,
    DateTime ChangedAtUtc,
    string ProfileAddr)
    : DomainEvent(EventId, ChangedAtUtc);
