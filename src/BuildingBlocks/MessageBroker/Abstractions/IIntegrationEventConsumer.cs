using MassTransit;

namespace MessageBroker.Abstractions;

public interface IIntegrationEventConsumer<in TIntegrationEvent>
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
}