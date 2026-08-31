using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class PlaceBoughtDomainEventHandlerTests
{
    [Fact]
    public async Task Increments_inviter_personal_volume_without_reactivating_invite()
    {
        var repository = new Repository();
        var handler = new PlaceBoughtDomainEventHandler(repository);

        await handler.Handle(new PlaceBoughtDomainEvent(
            marketingAddr: "marketing",
            structureNumber: 2,
            profileAddr: "referral-profile",
            placeNumber: 1,
            boughtAt: 10), default);

        Assert.False(repository.ReferralInvite.IsActive);
        Assert.Null(repository.ReferralInvite.ActivatedAt);
        Assert.Equal<uint>(1, repository.InviterFirstPlace.PersonalVolume);
    }

    private sealed class Repository : PlaceRepositoryStub
    {
        public Place ReferralInvite { get; } = CreatePlace(
            structureNumber: 0,
            profileAddr: "referral-profile",
            profileLogin: "referral",
            parentProfileAddr: "inviter-profile",
            parentProfileLogin: "inviter",
            isActive: false,
            activatedAt: null);

        public Place InviterFirstPlace { get; } = CreatePlace(
            structureNumber: 2,
            profileAddr: "inviter-profile",
            profileLogin: "inviter",
            parentProfileAddr: "root-profile",
            parentProfileLogin: "root",
            isActive: true,
            activatedAt: 1);

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken)
        {
            Place? result = (structureNumber, profileAddr, placeNumber) switch
            {
                (0, "referral-profile", 1) => ReferralInvite,
                (2, "inviter-profile", 1) => InviterFirstPlace,
                _ => null
            };
            return Task.FromResult(result);
        }
    }

    private static Place CreatePlace(
        byte structureNumber,
        string profileAddr,
        string profileLogin,
        string parentProfileAddr,
        string parentProfileLogin,
        bool isActive,
        long? activatedAt) =>
        Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber,
            profileAddr,
            profileLogin,
            index: profileLogin + "1",
            placeNumber: 1,
            parentProfileAddr,
            parentProfileLogin,
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: 0,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive,
            createdAt: 1,
            activatedAt,
            personalVolume: 0,
            groupVolume: 0);
}
