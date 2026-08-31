using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class CompressStructureRequestConsumer(
    IStructureCompressionService compressionService)
    : IConsumer<CompressStructureRequest>
{
    public async Task Consume(ConsumeContext<CompressStructureRequest> context)
    {
        var error = await compressionService.CompressAsync(
            context.Message.MarketingAddress,
            context.Message.StructureNumber,
            context.CancellationToken);
        await context.RespondAsync(new IntegrationRequestResponse(
            error is null ? null : [error]));
    }
}
