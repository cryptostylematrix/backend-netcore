using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

internal sealed class PlaceActivatedDomainEventHandler(IPlaceRepository placeRepository)
    : IDomainEventHandler<PlaceActivatedDomainEvent>
{
    private const byte InviteStructureNumber = 0;
    private const uint FirstPlaceNumber = 1;

    public async Task Handle(
        PlaceActivatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var invite = await placeRepository.GetAsync(
            notification.MarketingAddr,
            InviteStructureNumber,
            notification.ProfileAddr,
            FirstPlaceNumber,
            cancellationToken);

        if (invite is null)
            throw new InvalidOperationException("The profile invite place was not found.");

        if (invite.ParentProfileAddr is null)
            return;

        var curatorFirstPlace = await placeRepository.GetAsync(
            notification.MarketingAddr,
            notification.StructureNumber,
            invite.ParentProfileAddr,
            FirstPlaceNumber,
            cancellationToken);

        curatorFirstPlace?.IncreasePersonalVolume();
    }
}
