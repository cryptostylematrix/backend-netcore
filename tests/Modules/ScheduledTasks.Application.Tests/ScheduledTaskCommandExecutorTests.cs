using Ardalis.Result;
using IntegrationRequests;
using MessageBroker.Abstractions;
using ScheduledTasks.Application;
using ScheduledTasks.Core.TaskAggregate;

namespace ScheduledTasks.Application.Tests;

public sealed class ScheduledTaskCommandExecutorTests
{
    [Fact]
    public async Task Stops_after_first_failed_command()
    {
        var dispatcher = new RecordingDispatcher(failAtCall: 2);
        var executor = new ScheduledTaskCommandExecutor(
            new TaskCommandDocumentParser(),
            dispatcher,
            [new TestRequestFactory()]);
        var task = CreateTask();

        var result = await executor.ExecuteAsync(task, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, dispatcher.Requests.Count);
    }

    [Fact]
    public async Task Retry_dispatches_the_same_correlation_ids()
    {
        var dispatcher = new RecordingDispatcher();
        var executor = new ScheduledTaskCommandExecutor(
            new TaskCommandDocumentParser(),
            dispatcher,
            [new TestRequestFactory()]);
        var task = CreateTask();

        await executor.ExecuteAsync(task, default);
        var firstRun = dispatcher.Requests.Select(request => request.CorrelationId).ToArray();
        dispatcher.Requests.Clear();
        await executor.ExecuteAsync(task, default);

        Assert.Equal(firstRun, dispatcher.Requests.Select(request => request.CorrelationId));
    }

    private static ScheduledTask CreateTask() => ScheduledTask.Create(
        Guid.Parse("9ef7f182-ce82-405d-a247-dd5684725f8e"),
        DateTimeOffset.Parse("2030-01-01T00:00:00Z"),
        null,
        """
        [
          {"module":"test","type":"test.first","arguments":{}},
          {"module":"test","type":"test.second","arguments":{}},
          {"module":"test","type":"test.third","arguments":{}}
        ]
        """,
        DateTimeOffset.Parse("2029-01-01T00:00:00Z"));

    private sealed record TestRequest(
        Guid CorrelationId,
        DateTime OccurredOnUtc) : IIntegrationRequest;

    private sealed class TestRequestFactory : ITaskCommandRequestFactory
    {
        public bool CanCreate(TaskCommandEnvelope command) => command.Module == "test";

        public IIntegrationRequest Create(
            TaskCommandEnvelope command,
            Guid correlationId,
            DateTime occurredOnUtc) => new TestRequest(correlationId, occurredOnUtc);
    }

    private sealed class RecordingDispatcher(int? failAtCall = null)
        : IIntegrationRequestDispatcher
    {
        public List<IIntegrationRequest> Requests { get; } = [];

        public Task<Result> DispatchAsync(
            IIntegrationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(failAtCall == Requests.Count
                ? Result.Error("failed")
                : Result.Success());
        }
    }
}
