namespace MessageBroker.Abstractions;

public interface IIntegrationRequest
{
    Guid CorrelationId { get; init; }
    
    DateTime OccurredOnUtc { get; init; }
}