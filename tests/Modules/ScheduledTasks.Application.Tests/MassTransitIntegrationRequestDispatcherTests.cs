using IntegrationRequests;
using MassTransit;
using MessageBroker;
using MessageBroker.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ScheduledTasks.Infrastructure;

namespace ScheduledTasks.Application.Tests;

public sealed class MassTransitIntegrationRequestDispatcherTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Dispatches_through_in_memory_consumer_and_returns_response(
        bool fail,
        bool expectedSuccess)
    {
        var services = new ServiceCollection();
        services.AddMessageBroker(registration =>
            registration.AddConsumer<TestIntegrationRequestConsumer>());
        services.AddScoped<IIntegrationRequestDispatcher,
            MassTransitIntegrationRequestDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider
                .GetRequiredService<IIntegrationRequestDispatcher>();

            var result = await dispatcher.DispatchAsync(
                new TestIntegrationRequest(
                    fail,
                    Guid.Parse("dd202b33-5eca-4adf-903f-89e9dd088dd2"),
                    DateTime.Parse("2030-01-01T00:00:00Z").ToUniversalTime()));

            Assert.Equal(expectedSuccess, result.IsSuccess);
            if (fail)
                Assert.Contains("consumer failure", result.Errors);
        }
        finally
        {
            await bus.StopAsync();
        }
    }
}

public sealed record TestIntegrationRequest(
    bool Fail,
    Guid CorrelationId,
    DateTime OccurredOnUtc) : IIntegrationRequest;

public sealed class TestIntegrationRequestConsumer
    : IConsumer<TestIntegrationRequest>
{
    public Task Consume(ConsumeContext<TestIntegrationRequest> context) =>
        context.RespondAsync(new IntegrationRequestResponse(
            context.Message.Fail ? ["consumer failure"] : null));
}
