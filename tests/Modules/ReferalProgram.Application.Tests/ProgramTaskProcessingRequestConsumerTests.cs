using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.IntegrationRequests;
using ReferalProgram.Core.ProgramAggregate;
using ReferalProgramAggregate = ReferalProgram.Core.ProgramAggregate.ReferalProgram;

namespace ReferalProgram.Application.Tests;

public sealed class ProgramTaskProcessingRequestConsumerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sets_program_task_processing_state(bool isEnabled)
    {
        var program = ReferalProgramAggregate.Create("EQ_TEST");
        var repository = new StubReferalProgramRepository(program);
        var unitOfWork = new StubProgramUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton<IReferalProgramRepository>(repository);
        services.AddSingleton<IProgramUnitOfWork>(unitOfWork);
        services.AddMessageBroker(registration =>
        {
            registration.AddConsumer<DisableProgramTaskProcessingRequestConsumer>();
            registration.AddConsumer<EnableProgramTaskProcessingRequestConsumer>();
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var clientFactory = provider.GetRequiredService<IClientFactory>();
            var correlationId = Guid.Parse("9b940a35-723f-47a2-a277-e827e467d634");
            var occurredOnUtc = DateTime.Parse("2030-01-01T00:00:00Z").ToUniversalTime();

            Response<IntegrationRequestResponse> response;
            if (isEnabled)
            {
                var client = clientFactory
                    .CreateRequestClient<EnableProgramTaskProcessingRequest>();
                response = await client.GetResponse<IntegrationRequestResponse>(
                    new EnableProgramTaskProcessingRequest(
                        "EQ_TEST",
                        correlationId,
                        occurredOnUtc));
            }
            else
            {
                var client = clientFactory
                    .CreateRequestClient<DisableProgramTaskProcessingRequest>();
                response = await client.GetResponse<IntegrationRequestResponse>(
                    new DisableProgramTaskProcessingRequest(
                        "EQ_TEST",
                        correlationId,
                        occurredOnUtc));
            }

            Assert.Null(response.Message.Errors);
            Assert.Equal(isEnabled, program.IsTaskProcessingEnabled);
            Assert.Equal(1, unitOfWork.SaveCount);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private sealed class StubReferalProgramRepository(
        ReferalProgramAggregate program)
        : IReferalProgramRepository
    {
        public Task<ReferalProgramAggregate?> GetAsync(
            string marketingAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult<ReferalProgramAggregate?>(
                marketingAddr == program.MarketingAddr ? program : null);
    }

    private sealed class StubProgramUnitOfWork : IProgramUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }
}
