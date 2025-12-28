namespace IntegrationEvents;

public sealed record BuyPlaceTaskFoundIntegrationEvent(
    Guid EventId, 
    int TaskKey, 
    long QueryId, 
    short M,
    string RootPlaceAddr,
    string ProfileAddr,
    string ProfileLogin,
    string? InviterProfileAddr,
    string SourceAddr,
    string? ParentAddr, 
    short? Pos,
    DateTime OccurredOnUtc) : IIntegrationEvent;