using Common.Domain;
using ReferalProgram.Core.MarketingTaskAggregate;

namespace ReferalProgram.Application.Features.MarketingTasks;

public sealed record RecordMarketingTaskResponseAttemptCommand(
    string MarketingAddr,
    int TaskKey) : ICommand;

internal sealed class RecordMarketingTaskResponseAttemptCommandHandler(
    IMarketingTaskRepository repository,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<RecordMarketingTaskResponseAttemptCommand>
{
    public async Task<Result> Handle(
        RecordMarketingTaskResponseAttemptCommand request,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetAsync(
            request.MarketingAddr,
            request.TaskKey,
            cancellationToken);
        if (task is null)
            return Result.Error("Marketing task receipt was not found.");

        task.RecordResponseAttempt(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
