using Ardalis.Result;
using ScheduledTasks.Application.Abstractions;
using ScheduledTasks.Core.Scheduling;
using ScheduledTasks.Core.TaskAggregate;
using System.Text.Json;

namespace ScheduledTasks.Application;

public interface IScheduledTaskRunner
{
    Task<bool> RunNextAsync(CancellationToken cancellationToken);
}

public sealed class ScheduledTaskRunner(
    IScheduledTaskRepository repository,
    IScheduledTasksUnitOfWork unitOfWork,
    ScheduledTaskCommandExecutor executor,
    TaskScheduleCalculator scheduleCalculator,
    TimeProvider timeProvider) : IScheduledTaskRunner
{
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        var task = await repository.GetNextDueAsync(
            timeProvider.GetUtcNow(),
            cancellationToken);
        if (task is null)
            return false;

        Result result;
        try
        {
            result = await executor.ExecuteAsync(task, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            task.MarkFailed(exception.Message, timeProvider.GetUtcNow());
            await unitOfWork.TrySaveChangesAsync(CancellationToken.None);
            return true;
        }

        var finishedAt = timeProvider.GetUtcNow();
        if (!result.IsSuccess)
        {
            task.MarkFailed(string.Join("; ", result.Errors), finishedAt);
            await unitOfWork.TrySaveChangesAsync(cancellationToken);
            return true;
        }

        DateTimeOffset? nextExecution;
        try
        {
            nextExecution = scheduleCalculator.GetNextExecution(
                task.Schedule,
                task.ExecuteAtUtc!.Value,
                finishedAt);
        }
        catch (Exception exception) when (
            exception is JsonException or FormatException or OverflowException)
        {
            task.MarkFailed(exception.Message, finishedAt);
            await unitOfWork.TrySaveChangesAsync(cancellationToken);
            return true;
        }

        task.Complete(nextExecution, finishedAt);
        await unitOfWork.TrySaveChangesAsync(cancellationToken);
        return true;
    }
}
