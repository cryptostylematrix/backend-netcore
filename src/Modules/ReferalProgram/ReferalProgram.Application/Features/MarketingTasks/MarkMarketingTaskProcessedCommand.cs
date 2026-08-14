using Common.Domain;
using ReferalProgram.Core.MarketingTaskAggregate;

namespace ReferalProgram.Application.Features.MarketingTasks;

public sealed record MarkMarketingTaskProcessedCommand(
    string MarketingAddr,
    int TaskKey,
    long TaskQueryId) : ICommand;

internal sealed class MarkMarketingTaskProcessedCommandHandler(
    IMarketingTaskRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MarkMarketingTaskProcessedCommand>
{
    public async Task<Result> Handle(
        MarkMarketingTaskProcessedCommand request,
        CancellationToken cancellationToken)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var task = await repository.GetAsync(
            request.MarketingAddr,
            request.TaskKey,
            cancellationToken);

        if (task is null)
        {
            task = MarketingTask.Complete(
                request.MarketingAddr,
                request.TaskKey,
                request.TaskQueryId,
                completedAt);
            repository.Add(task);
        }
        else
        {
            task.MarkCompleted(request.TaskQueryId, completedAt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
