using MassTransit;

namespace MessageBroker.Abstractions;

public interface IIntegrationRequestConsumer<in TIntegrationRequest>
    : IConsumer<TIntegrationRequest>
    where TIntegrationRequest : class, IIntegrationRequest
{
}