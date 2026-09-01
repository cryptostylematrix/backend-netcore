using IntegrationRequests;
using MassTransit;
using MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.IntegrationRequests;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.PlaceAggregate;
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
        var failedTask = isEnabled ? CreateFailedTask() : null;
        var marketingTasks = new StubMarketingTaskRepository(failedTask);
        var unitOfWork = new StubProgramUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton<IReferalProgramRepository>(repository);
        services.AddSingleton<IMarketingTaskRepository>(marketingTasks);
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
            Assert.Equal(isEnabled ? 1 : 0, marketingTasks.FailedLookupCount);
            if (failedTask is not null)
            {
                Assert.Null(failedTask.ResponseAttemptedAt);
                Assert.Null(failedTask.ErrorAt);
                Assert.Null(failedTask.ErrorReason);
            }
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

    private static MarketingTask CreateFailedTask()
    {
        var place = Place.Create(
            1, "EQ_TEST", 1, "profile", "profile", "profile1", 1,
            "parent", "parent", 1, "00000001", 0, 0, 1, 0, 1,
            true, 1, 1);
        var task = MarketingTask.RecordProcessedCommand(
            "EQ_TEST", 1, 1, "wallet", place, place, 0, DateTimeOffset.UtcNow);
        task.RecordResponseAttempt(DateTimeOffset.UtcNow);
        task.MarkDeliveryError("contract_rejected_response", DateTimeOffset.UtcNow);
        return task;
    }

    private sealed class StubMarketingTaskRepository(MarketingTask? failedTask)
        : IMarketingTaskRepository
    {
        public int FailedLookupCount { get; private set; }

        public Task<MarketingTask?> GetAsync(
            string marketingAddr,
            int taskKey,
            CancellationToken cancellationToken) => Task.FromResult<MarketingTask?>(null);

        public Task<MarketingTask?> GetFailedAsync(
            string marketingAddr,
            CancellationToken cancellationToken)
        {
            FailedLookupCount++;
            return Task.FromResult(failedTask);
        }

        public void Add(MarketingTask task)
        {
        }
    }
}
