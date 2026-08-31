using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class ResetStructureActivaityRequestConsumer(
    IPlaceRepository placeRepository,
    IProgramUnitOfWork unitOfWork)
    : IConsumer<ResetStructureActivaityRequest>
{
    public async Task Consume(
        ConsumeContext<ResetStructureActivaityRequest> context)
    {
        if (context.Message.StructureNumber is < byte.MinValue or > byte.MaxValue)
        {
            await context.RespondAsync(new IntegrationRequestResponse(
            [
                $"Structure number {context.Message.StructureNumber} is outside the byte range."
            ]));
            return;
        }

        var places = await placeRepository.GetStructurePlacesAsync(
            context.Message.MarketingAddress,
            checked((byte)context.Message.StructureNumber),
            context.CancellationToken);

        foreach (var place in places)
            place.ResetActivity();

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
