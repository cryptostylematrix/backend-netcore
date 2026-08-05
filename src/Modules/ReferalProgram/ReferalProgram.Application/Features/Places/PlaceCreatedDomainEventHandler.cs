using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

internal sealed class PlaceCreatedDomainEventHandler(IPlaceRepository placeRepository)
    : IDomainEventHandler<PlaceCreatedDomainEvent>
{
    public async Task Handle(
        PlaceCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var parent = await placeRepository.GetByIdAsync(
            notification.ParentId,
            cancellationToken);

        if (parent is null)
            throw new InvalidOperationException("The parent place was not found.");

        parent.RegisterChild(notification.ExpectedParentFilling);
    }
}
