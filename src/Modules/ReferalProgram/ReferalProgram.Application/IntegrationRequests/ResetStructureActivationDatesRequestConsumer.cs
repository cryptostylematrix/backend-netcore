using IntegrationRequests;
using MassTransit;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class ResetStructureActivationDatesRequestConsumer
    : IConsumer<ResetStructureActivationDatesRequest>
{
    public async Task Consume(
        ConsumeContext<ResetStructureActivationDatesRequest> context)
    {
        // TODO: Implement resetting structure activation dates.

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
