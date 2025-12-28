using MassTransit;
using MessageBroker.Abstractions;

namespace MessageBroker;


public sealed class EventBus(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent message,
        CancellationToken cancellationToken = default) where TIntegrationEvent : IIntegrationEvent
    {
        await publishEndpoint.Publish(message, cancellationToken);
    }
}