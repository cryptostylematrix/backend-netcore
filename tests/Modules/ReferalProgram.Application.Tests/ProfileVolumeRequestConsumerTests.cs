using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.IntegrationRequests;

namespace ReferalProgram.Application.Tests;

public sealed class ProfileVolumeRequestConsumerTests
{
    [Fact]
    public async Task Calculates_referral_volume_for_requested_structure()
    {
        var maintenance = new Maintenance();
        await using var provider = Provider(maintenance, registration =>
            registration.AddConsumer<CalculateStructureReferralVolumeRequestConsumer>());
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<CalculateStructureReferralVolumeRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                new CalculateStructureReferralVolumeRequest(
                    "marketing", 2, Guid.NewGuid(), DateTime.UtcNow));

            Assert.Null(response.Message.Errors);
            Assert.Equal(("marketing", (byte)2), maintenance.Calculated);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Fact]
    public async Task Resets_referral_volume_for_requested_structure()
    {
        var maintenance = new Maintenance();
        await using var provider = Provider(maintenance, registration =>
            registration.AddConsumer<ResetStructureReferralVolumeRequestConsumer>());
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = provider.GetRequiredService<IClientFactory>()
                .CreateRequestClient<ResetStructureReferralVolumeRequest>();
            var response = await client.GetResponse<IntegrationRequestResponse>(
                new ResetStructureReferralVolumeRequest(
                    "marketing", 3, Guid.NewGuid(), DateTime.UtcNow));

            Assert.Null(response.Message.Errors);
            Assert.Equal(("marketing", (byte)3), maintenance.Reset);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private static ServiceProvider Provider(
        IProfileVolumeMaintenance maintenance,
        Action<IRegistrationConfigurator> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(maintenance);
        services.AddMessageBroker(configure);
        return services.BuildServiceProvider();
    }

    private sealed class Maintenance : IProfileVolumeMaintenance
    {
        public (string, byte)? Calculated { get; private set; }
        public (string, byte)? Reset { get; private set; }

        public Task RecalculateReferralAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken)
        {
            Calculated = (marketingAddr, structureNumber);
            return Task.CompletedTask;
        }

        public Task ResetReferralAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken)
        {
            Reset = (marketingAddr, structureNumber);
            return Task.CompletedTask;
        }
    }
}
