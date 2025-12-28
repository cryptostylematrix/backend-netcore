namespace MessageBroker.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent message,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}