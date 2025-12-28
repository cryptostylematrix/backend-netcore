namespace MessageBroker.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; init; }
    
    DateTime OccurredOnUtc { get; init; }
}