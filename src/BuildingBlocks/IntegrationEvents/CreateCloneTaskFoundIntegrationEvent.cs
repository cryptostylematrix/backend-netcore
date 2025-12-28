namespace IntegrationEvents;

public sealed record CreateCloneTaskFoundIntegrationEvent(
    int TaskKey, 
    long QueryId, 
    short M, 
    string ProfileAddr,
    Guid EventId, 
    DateTime OccurredOnUtc) : IIntegrationEvent;