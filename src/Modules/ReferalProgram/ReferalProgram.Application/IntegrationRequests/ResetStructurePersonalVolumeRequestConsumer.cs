using IntegrationRequests;
using MassTransit;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class ResetStructurePersonalVolumeRequestConsumer
    : IConsumer<ResetStructurePersonalVolumeRequest>
{
    public async Task Consume(
        ConsumeContext<ResetStructurePersonalVolumeRequest> context)
    {
        // TODO: Implement structure personal-volume reset.

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
