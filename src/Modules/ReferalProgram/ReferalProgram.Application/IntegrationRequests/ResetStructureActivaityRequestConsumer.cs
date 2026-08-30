using IntegrationRequests;
using MassTransit;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class ResetStructureActivaityRequestConsumer
    : IConsumer<ResetStructureActivaityRequest>
{
    public async Task Consume(
        ConsumeContext<ResetStructureActivaityRequest> context)
    {
        // TODO: Implement resetting structure activity.

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
