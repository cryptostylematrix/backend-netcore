using MassTransit;
using MessageBroker.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MessageBroker;

public static class MessageBrokerConfiguration
{
    public static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        params Action<IRegistrationConfigurator>[] configureConsumers)
    {
        services.TryAddScoped<IIntegrationEventPublisher, EventBus>();

        services.AddMassTransit(configure =>
        {
            foreach (var configureConsumer in configureConsumers)
                configureConsumer(configure);

            configure.SetKebabCaseEndpointNameFormatter();
            configure.UsingInMemory((context, bus) =>
                bus.ConfigureEndpoints(context));
        });

        return services;
    }
}
