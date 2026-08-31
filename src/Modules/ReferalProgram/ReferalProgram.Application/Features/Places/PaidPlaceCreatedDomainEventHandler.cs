using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

internal sealed class PaidPlaceCreatedDomainEventHandler(
    IPlaceRepository placeRepository,
    IPlaceQueries placeQueries)
    : IDomainEventHandler<PaidPlaceCreatedDomainEvent>
{
    public async Task Handle(
        PaidPlaceCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var alreadyHadPaidPlace =
            await placeQueries.HasProfilePlacesOutsideInviteStructureAsync(
                notification.MarketingAddr,
                notification.ProfileAddr,
                cancellationToken);
        if (alreadyHadPaidPlace)
            return;

        var invite = await placeRepository.GetAsync(
            notification.MarketingAddr,
            structureNumber: 0,
            notification.ProfileAddr,
            placeNumber: 1,
            cancellationToken);

        if (invite is null)
            throw new InvalidOperationException("The profile invite place was not found.");

        invite.InitializeActivityFromFirstPaidPlace(notification.CreatedAt);
    }
}
