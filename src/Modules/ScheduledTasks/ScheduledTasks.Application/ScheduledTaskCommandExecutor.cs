using Ardalis.Result;
using IntegrationRequests;
using MessageBroker.Abstractions;
using ScheduledTasks.Core.TaskAggregate;
using System.Text.Json;

namespace ScheduledTasks.Application;

public sealed class ScheduledTaskCommandExecutor(
    TaskCommandDocumentParser parser,
    IIntegrationRequestDispatcher dispatcher,
    IEnumerable<ITaskCommandRequestFactory> requestFactories)
{
    public async Task<Result> ExecuteAsync(
        ScheduledTask task,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TaskCommandEnvelope> commands;
        try
        {
            commands = parser.Parse(task.Commands);
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return Result.Error(exception.Message);
        }

        foreach (var command in commands)
        {
            IIntegrationRequest request;
            try
            {
                request = CreateRequest(task, command);
            }
            catch (FormatException exception)
            {
                return Result.Error(exception.Message);
            }

            var result = await dispatcher.DispatchAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return result;
        }

        return Result.Success();
    }

    private IIntegrationRequest CreateRequest(
        ScheduledTask task,
        TaskCommandEnvelope command)
    {
        var correlationId = DeterministicCorrelationId.Create(
            task.Id,
            task.ExecutionNumber,
            command.Sequence);
        var occurredOnUtc = task.ExecuteAtUtc!.Value.UtcDateTime;

        var factories = requestFactories
            .Where(factory => factory.CanCreate(command))
            .ToArray();
        return factories.Length switch
        {
            1 => factories[0].Create(command, correlationId, occurredOnUtc),
            0 => throw new FormatException(
                $"Command {command.Sequence} has unsupported module/type " +
                $"'{command.Module}/{command.Type}' version {command.Version}."),
            _ => throw new FormatException(
                $"Command {command.Sequence} has multiple registered request factories.")
        };
    }
}
