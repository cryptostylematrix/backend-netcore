using Common.Domain;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.MarketingTasks;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProgramAggregate;
using ReferalProgram.Dto;
using ReferalProgramAggregate = ReferalProgram.Core.ProgramAggregate.ReferalProgram;

namespace ReferalProgram.Application.Tests;

public sealed class MarketingTaskTests
{
    [Fact]
    public void Place_records_processed_marketing_command_as_domain_event()
    {
        var place = CreatePlace("buyer", 2);
        var source = CreatePlace("source", 1);
        var processedAt = DateTimeOffset.Parse("2026-08-14T10:00:00Z");

        place.RecordProcessedMarketingCommand(
            taskKey: 12,
            taskQueryId: 34,
            taskSourceAddr: "wallet",
            responseSourcePlace: source,
            responseCode: 7,
            processedAt);

        var domainEvent = Assert.Single(
            place.DomainEvents.OfType<MarketingCommandProcessedDomainEvent>());
        Assert.Equal("marketing", domainEvent.MarketingAddr);
        Assert.Equal(12, domainEvent.TaskKey);
        Assert.Equal(34, domainEvent.TaskQueryId);
        Assert.Equal("wallet", domainEvent.TaskSourceAddr);
        Assert.Same(place, domainEvent.Place);
        Assert.Same(source, domainEvent.ResponseSourcePlace);
        Assert.Equal<uint>(7, domainEvent.ResponseCode);
        Assert.Equal(processedAt, domainEvent.ProcessedAt);
    }

    [Fact]
    public void Processed_command_keeps_identity_affected_place_source_and_exact_code()
    {
        var place = CreatePlace("buyer", 2);
        var source = CreatePlace("source", 1);

        var task = MarketingTask.RecordProcessedCommand(
            "marketing",
            12,
            34,
            "wallet",
            place,
            source,
            responseCode: 7,
            DateTimeOffset.Parse("2026-08-14T10:00:00Z"));

        Assert.Equal("marketing", task.MarketingAddr);
        Assert.Equal(12, task.TaskKey);
        Assert.Equal(34, task.TaskQueryId);
        Assert.Same(place, task.Place);
        Assert.Same(source, task.ResponseSourcePlace);
        Assert.Equal<uint>(7, task.ResponseCode);
    }

    [Fact]
    public async Task Receipt_query_returns_stored_command_response_without_recalculation()
    {
        var source = CreatePlace("source", 1);
        var repository = new Repository
        {
            Existing = MarketingTask.RecordProcessedCommand(
                "marketing",
                12,
                34,
                "wallet",
                CreatePlace("buyer", 2),
                source,
                responseCode: 9,
                DateTimeOffset.UtcNow)
        };
        var handler = new GetMarketingTaskQueryHandler(repository);

        var result = await handler.Handle(
            new GetMarketingTaskQuery("marketing", 12),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(34, result.Value?.TaskQueryId);
        Assert.Equal<uint>(9, result.Value!.CommandResponse.Code);
        Assert.Equal("source", result.Value.CommandResponse.Source.ProfileAddr);
        Assert.Equal<uint>(1, result.Value.CommandResponse.Source.PlaceNumber);
    }

    [Fact]
    public async Task Domain_event_handler_adds_processed_receipt_without_saving()
    {
        var place = CreatePlace("buyer", 2);
        var source = CreatePlace("source", 1);
        var taskRepository = new Repository();
        var handler = new MarketingCommandProcessedDomainEventHandler(taskRepository);

        await handler.Handle(new MarketingCommandProcessedDomainEvent(
            "marketing",
            12,
            34,
            "wallet",
            place,
            source,
            11,
            DateTimeOffset.UtcNow),
            default);

        Assert.Equal("wallet", taskRepository.Added?.TaskSourceAddr);
        Assert.Same(place, taskRepository.Added?.Place);
        Assert.Same(source, taskRepository.Added?.ResponseSourcePlace);
        Assert.Equal<uint>(11, taskRepository.Added!.ResponseCode);
    }

    [Fact]
    public async Task Response_attempt_handler_records_attempt_and_saves()
    {
        var task = CreateTask();
        var unitOfWork = new UnitOfWork();
        var handler = new RecordMarketingTaskResponseAttemptCommandHandler(
            new Repository { Existing = task },
            unitOfWork);

        var result = await handler.Handle(
            new RecordMarketingTaskResponseAttemptCommand("marketing", 12),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(task.ResponseAttemptedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Delivery_failure_handler_marks_task_and_disables_program_atomically()
    {
        var task = CreateTask();
        task.RecordResponseAttempt(DateTimeOffset.UtcNow);
        var program = ReferalProgramAggregate.Create("marketing");
        var unitOfWork = new UnitOfWork();
        var handler = new FailMarketingTaskDeliveryCommandHandler(
            new Repository { Existing = task },
            new ProgramRepository(program),
            unitOfWork);

        var result = await handler.Handle(
            new FailMarketingTaskDeliveryCommand(
                "marketing",
                12,
                "contract_rejected_response"),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(task.ErrorAt);
        Assert.Equal("contract_rejected_response", task.ErrorReason);
        Assert.False(program.IsTaskProcessingEnabled);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public void Manual_reset_keeps_receipt_but_clears_delivery_state()
    {
        var task = CreateTask();
        task.RecordResponseAttempt(DateTimeOffset.UtcNow);
        task.MarkDeliveryError("contract_rejected_response", DateTimeOffset.UtcNow);

        task.ResetDeliveryFailure();

        Assert.Equal(12, task.TaskKey);
        Assert.Null(task.ResponseAttemptedAt);
        Assert.Null(task.ErrorAt);
        Assert.Null(task.ErrorReason);
    }

    private sealed class Repository : IMarketingTaskRepository
    {
        public MarketingTask? Existing { get; init; }
        public MarketingTask? Added { get; private set; }
        public (string MarketingAddr, int TaskKey)? LastLookup { get; private set; }

        public Task<MarketingTask?> GetAsync(
            string marketingAddr,
            int taskKey,
            CancellationToken cancellationToken)
        {
            LastLookup = (marketingAddr, taskKey);
            return Task.FromResult(Existing);
        }

        public Task<MarketingTask?> GetFailedAsync(
            string marketingAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult(Existing?.ErrorAt is not null ? Existing : null);

        public void Add(MarketingTask task) => Added = task;
    }

    private static Place CreatePlace(string profileAddr, uint placeNumber) => Place.Create(
        parentId: 1,
        marketingAddr: "marketing",
        structureNumber: 2,
        profileAddr,
        profileLogin: profileAddr,
        index: profileAddr + placeNumber,
        placeNumber,
        parentProfileAddr: "parent",
        parentProfileLogin: "parent",
        parentPlaceNumber: 1,
        mp: "0000000000000001",
        posGroup: 0,
        kind: PlaceKinds.Purchased,
        pos: 1,
        filling: 0,
        deep: 2,
        isActive: true,
        createdAt: 1,
        activatedAt: 1,
        personalVolume: 0,
        groupVolume: 0);

    private static MarketingTask CreateTask() => MarketingTask.RecordProcessedCommand(
        "marketing",
        12,
        34,
        "wallet",
        CreatePlace("buyer", 2),
        CreatePlace("source", 1),
        responseCode: 7,
        DateTimeOffset.UtcNow);

    private sealed class ProgramRepository(ReferalProgramAggregate program)
        : IReferalProgramRepository
    {
        public Task<ReferalProgramAggregate?> GetAsync(
            string marketingAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult<ReferalProgramAggregate?>(program);
    }

    private sealed class UnitOfWork : IProgramUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }

}
