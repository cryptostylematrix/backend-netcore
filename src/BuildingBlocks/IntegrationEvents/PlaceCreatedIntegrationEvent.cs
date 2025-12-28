namespace IntegrationEvents;

public sealed record PlaceCreatedIntegrationEvent(
    Guid EventId, 
    int TaskKey,
    long QueryId,
    short M,
    int PlaceNumber,
    short IsClone,
    string ProfileAddr,
    string? InviterProfileAddr,
    string ParentAddr,
    DateTime OccurredOnUtc) : IIntegrationEvent;