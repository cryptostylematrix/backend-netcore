using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class PaidPlaceCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task First_paid_place_activates_invite()
    {
        var repository = new Repository();
        var handler = new PaidPlaceCreatedDomainEventHandler(
            repository,
            new Places(hasExistingPaidPlace: false));

        await handler.Handle(
            new PaidPlaceCreatedDomainEvent("marketing", "profile", 10),
            default);

        Assert.True(repository.Invite.IsActive);
        Assert.Equal(10, repository.Invite.ActivatedAt);
    }

    [Fact]
    public async Task Later_paid_place_does_not_reactivate_reset_invite()
    {
        var repository = new Repository();
        var handler = new PaidPlaceCreatedDomainEventHandler(
            repository,
            new Places(hasExistingPaidPlace: true));

        await handler.Handle(
            new PaidPlaceCreatedDomainEvent("marketing", "profile", 10),
            default);

        Assert.False(repository.Invite.IsActive);
        Assert.Null(repository.Invite.ActivatedAt);
    }

    private sealed class Places(bool hasExistingPaidPlace) : PlaceQueriesStub
    {
        public override Task<bool> HasProfilePlacesOutsideInviteStructureAsync(
            string marketingAddr,
            string profileAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult(hasExistingPaidPlace);
    }

    private sealed class Repository : PlaceRepositoryStub
    {
        public Place Invite { get; } = Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 0,
            profileAddr: "profile",
            profileLogin: "profile",
            index: "profile1",
            placeNumber: 1,
            parentProfileAddr: "curator",
            parentProfileLogin: "curator",
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: false,
            createdAt: 1,
            activatedAt: null);

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<Place?>(Invite);
    }
}
