using IntegrationRequests;
using MassTransit;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class UpdateStructureActivityRequestConsumer
    : IConsumer<UpdateStructureActivityRequest>
{
    public async Task Consume(ConsumeContext<UpdateStructureActivityRequest> context)
    {
        // TODO: Implement structure activity update.

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
