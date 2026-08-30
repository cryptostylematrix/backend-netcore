using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.ProgramAggregate;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class DisableProgramTaskProcessingRequestConsumer(
    IReferalProgramRepository repository,
    IProgramUnitOfWork unitOfWork)
    : IConsumer<DisableProgramTaskProcessingRequest>
{
    public async Task Consume(
        ConsumeContext<DisableProgramTaskProcessingRequest> context)
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

        program.DisableTaskProcessing();
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
