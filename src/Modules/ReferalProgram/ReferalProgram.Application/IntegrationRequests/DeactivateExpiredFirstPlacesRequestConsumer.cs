using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class DeactivateExpiredFirstPlacesRequestConsumer(
    ExpiredFirstPlaceDeactivationService service) : IConsumer<DeactivateExpiredFirstPlacesRequest>
{
    public async Task Consume(ConsumeContext<DeactivateExpiredFirstPlacesRequest> context)
    {
        try
        {
            await service.ExecuteAsync(context.Message, context.CancellationToken);
        }
        catch (Exception exception) when (exception is FormatException or NotSupportedException)
        {
            await context.RespondAsync(new IntegrationRequestResponse([exception.Message]));
            return;
        }

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
