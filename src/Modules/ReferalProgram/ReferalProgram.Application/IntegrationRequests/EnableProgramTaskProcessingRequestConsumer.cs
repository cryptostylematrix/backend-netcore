using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.ProgramAggregate;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class EnableProgramTaskProcessingRequestConsumer(
    IReferalProgramRepository repository,
    IMarketingTaskRepository marketingTaskRepository,
    IProgramUnitOfWork unitOfWork)
    : IConsumer<EnableProgramTaskProcessingRequest>
{
    public async Task Consume(
        ConsumeContext<EnableProgramTaskProcessingRequest> context)
    {
        var program = await repository.GetAsync(
            context.Message.MarketingAddress,
            context.CancellationToken);
        if (program is null)
        {
            await context.RespondAsync(new IntegrationRequestResponse(
            [
                $"Referral Program '{context.Message.MarketingAddress}' was not found."
            ]));
            return;
        }

        var failedTask = await marketingTaskRepository.GetFailedAsync(
            context.Message.MarketingAddress,
            context.CancellationToken);
        failedTask?.ResetDeliveryFailure();
        program.EnableTaskProcessing();
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
