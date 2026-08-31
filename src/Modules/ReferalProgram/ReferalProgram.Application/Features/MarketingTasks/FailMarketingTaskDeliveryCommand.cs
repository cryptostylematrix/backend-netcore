using Common.Domain;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.ProgramAggregate;

namespace ReferalProgram.Application.Features.MarketingTasks;

public sealed record FailMarketingTaskDeliveryCommand(
    string MarketingAddr,
    int TaskKey,
    string Reason) : ICommand;

internal sealed class FailMarketingTaskDeliveryCommandHandler(
    IMarketingTaskRepository taskRepository,
    IReferalProgramRepository programRepository,
    IProgramUnitOfWork unitOfWork)
    : ICommandHandler<FailMarketingTaskDeliveryCommand>
{
    public async Task<Result> Handle(
        FailMarketingTaskDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetAsync(
            request.MarketingAddr,
            request.TaskKey,
            cancellationToken);
        if (task is null)
            return Result.Error("Marketing task receipt was not found.");

        var program = await programRepository.GetAsync(
            request.MarketingAddr,
            cancellationToken);
        if (program is null)
            return Result.Error("Referral program was not found.");

        task.MarkDeliveryError(request.Reason, DateTimeOffset.UtcNow);
        program.DisableTaskProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
