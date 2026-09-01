using IntegrationRequests;
using MassTransit;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.IntegrationRequests;

public sealed class CalculateStructurePersonalVolumeRequestConsumer(
    IPlaceRepository placeRepository,
    IProgramUnitOfWork unitOfWork)
    : IConsumer<CalculateStructurePersonalVolumeRequest>
{
    public async Task Consume(
        ConsumeContext<CalculateStructurePersonalVolumeRequest> context)
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
        var inviters = await placeRepository.GetInvitersAsync(
            context.Message.MarketingAddress,
            context.CancellationToken);

        var paidPlacesByInviter = places
            .Where(place => place.IsActive && place.ProfileAddr is not null)
            .Select(place => inviters.TryGetValue(place.ProfileAddr!, out var inviter)
                ? inviter
                : null)
            .Where(inviter => inviter is not null)
            .GroupBy(inviter => inviter!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => checked((uint)group.Count()),
                StringComparer.Ordinal);

        foreach (var place in places)
        {
            var personalVolume = place.PlaceNumber == 1
                && place.ProfileAddr is not null
                && paidPlacesByInviter.TryGetValue(place.ProfileAddr, out var count)
                    ? count
                    : 0;
            place.SetPersonalVolume(personalVolume);
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(new IntegrationRequestResponse(null));
    }
}
