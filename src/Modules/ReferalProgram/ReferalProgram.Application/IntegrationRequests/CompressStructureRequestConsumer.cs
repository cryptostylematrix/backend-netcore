using IntegrationRequests;
using MassTransit;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class CompressStructureRequestConsumer
    : IConsumer<CompressStructureRequest>
{
    public async Task Consume(ConsumeContext<CompressStructureRequest> context)
    {
        // TODO: Implement structure compression.

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
