using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Abstractions;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class ResetStructureReferralVolumeRequestConsumer(
    IProfileVolumeMaintenance maintenance)
    : IConsumer<ResetStructureReferralVolumeRequest>
{
    public async Task Consume(
        ConsumeContext<ResetStructureReferralVolumeRequest> context)
    {
        if (context.Message.StructureNumber is < byte.MinValue or > byte.MaxValue)
        {
            await context.RespondAsync(new IntegrationRequestResponse(
            [
                $"Structure number {context.Message.StructureNumber} is outside the byte range."
            ]));
            return;
        }

        await maintenance.ResetReferralAsync(
            context.Message.MarketingAddress,
            checked((byte)context.Message.StructureNumber),
            context.CancellationToken);

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
