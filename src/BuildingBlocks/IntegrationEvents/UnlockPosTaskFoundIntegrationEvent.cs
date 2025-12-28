namespace IntegrationEvents;

public sealed record UnlockPosTaskFoundIntegrationEvent(
    int TaskKey,
    long QueryId,
    short M,
    string ProfileAddr,
    string ProfileOwnerAddr,
    string SourceAddr,
    string ParentAddr,
    short Pos,
    Guid EventId, 
    DateTime OccurredOnUtc) : IIntegrationEvent;