using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.IntegrationRequests;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Tests;

public sealed class ExpiredFirstPlaceDeactivationTests
{
    [Fact]
    public async Task Service_is_disabled_without_database_dependencies()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            new ExpiredFirstPlaceDeactivationService().ExecuteAsync(Request(), default));
        Assert.Equal(ExpiredFirstPlaceDeactivationService.DisabledMessage, exception.Message);
    }

    [Fact]
    public async Task Cancellation_is_not_reported_as_a_business_failure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ExpiredFirstPlaceDeactivationService().ExecuteAsync(Request(), cancellation.Token));
    }

    [Fact]
    public async Task Consumer_reports_disabled_instead_of_acknowledging_success()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ExpiredFirstPlaceDeactivationService>();
        services.AddMessageBroker(registration =>
            registration.AddConsumer<DeactivateExpiredFirstPlacesRequestConsumer>());
        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<DeactivateExpiredFirstPlacesRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(Request());
            Assert.NotNull(response.Message.Errors);
            Assert.Equal(ExpiredFirstPlaceDeactivationService.DisabledMessage,
                Assert.Single(response.Message.Errors));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private static DeactivateExpiredFirstPlacesRequest Request() => new(
        "marketing", 1, new ActivityExpirationPeriod("days", 1), Guid.NewGuid(),
        new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc));
}
