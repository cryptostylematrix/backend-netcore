using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Abstractions;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class CalculateStructureReferralVolumeRequestConsumer(
    IProfileVolumeMaintenance maintenance)
    : IConsumer<CalculateStructureReferralVolumeRequest>
{
    public async Task Consume(
        ConsumeContext<CalculateStructureReferralVolumeRequest> context)
    {
        if (context.Message.StructureNumber is < byte.MinValue or > byte.MaxValue)
        {
            await context.RespondAsync(new IntegrationRequestResponse(
            [
                $"Structure number {context.Message.StructureNumber} is outside the byte range."
            ]));
            return;
        }

        await maintenance.RecalculateReferralAsync(
            context.Message.MarketingAddress,
            checked((byte)context.Message.StructureNumber),
            context.CancellationToken);

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
