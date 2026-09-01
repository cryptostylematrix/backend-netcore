using Common.Domain;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Application.Features.ProfileVolumes;

internal sealed class ProfileVolumeOperationDomainEventHandler(
    IPlaceRepository placeRepository,
    IProfileVolumeRepository profileVolumeRepository,
    IProfileVolumeAmountPolicy amountPolicy)
    : IDomainEventHandler<ProfileVolumeOperationDomainEvent>
{
    private const byte InviteStructureNumber = 0;
    private const uint FirstPlaceNumber = 1;

    public async Task Handle(
        ProfileVolumeOperationDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var amount = amountPolicy.Resolve(notification.Operation);
        await profileVolumeRepository.IncreasePersonalAsync(
            notification.MarketingAddr,
            notification.StructureNumber,
            notification.ProfileAddr,
            amount,
            cancellationToken);

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

        await profileVolumeRepository.IncreaseReferralAsync(
            notification.MarketingAddr,
            notification.StructureNumber,
            invite.ParentProfileAddr,
            amount,
            cancellationToken);
    }
}
