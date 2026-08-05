using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

internal sealed class PlaceBoughtDomainEventHandler(IPlaceRepository placeRepository)
    : IDomainEventHandler<PlaceBoughtDomainEvent>
{
    private const byte InviteStructureNumber = 0;
    private const uint FirstPlaceNumber = 1;

    public async Task Handle(
        PlaceBoughtDomainEvent notification,
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

        invite.Activate(notification.BoughtAt);

        if (invite.ParentProfileAddr is null)
            return;

        var inviterFirstPlace = await placeRepository.GetAsync(
            notification.MarketingAddr,
            notification.StructureNumber,
            invite.ParentProfileAddr,
            FirstPlaceNumber,
            cancellationToken);

        inviterFirstPlace?.IncreasePersonalVolume();
    }
}
