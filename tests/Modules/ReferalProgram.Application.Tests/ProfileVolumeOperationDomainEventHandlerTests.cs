using ReferalProgram.Application.Features.ProfileVolumes;
using ReferalProgram.Application.Policies;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class ProfileVolumeOperationDomainEventHandlerTests
{
    [Theory]
    [InlineData(ProfileVolumeOperation.BuyFirstPlace)]
    [InlineData(ProfileVolumeOperation.BuyPlace)]
    [InlineData(ProfileVolumeOperation.ActivatePlace)]
    [InlineData(ProfileVolumeOperation.CreateClone)]
    [InlineData(ProfileVolumeOperation.CreateReinvest)]
    public async Task Increments_owner_personal_and_current_curator_referral_volume(
        ProfileVolumeOperation operation)
    {
        var repository = new Repository("curator");
        var volumes = new VolumeRepository();
        var handler = new ProfileVolumeOperationDomainEventHandler(
            repository,
            volumes,
            new ProfileVolumeAmountPolicy());

        await handler.Handle(new ProfileVolumeOperationDomainEvent(
            "marketing", 3, "profile", operation, 100), default);

        Assert.Equal([("marketing", (byte)3, "profile", 1u)], volumes.Personal);
        Assert.Equal([("marketing", (byte)3, "curator", 1u)], volumes.Referral);
    }

    [Fact]
    public async Task Root_profile_has_personal_volume_without_referral_volume()
    {
        var volumes = new VolumeRepository();
        var handler = new ProfileVolumeOperationDomainEventHandler(
            new Repository(parentProfileAddr: null),
            volumes,
            new ProfileVolumeAmountPolicy());

        await handler.Handle(new ProfileVolumeOperationDomainEvent(
            "marketing", 0, "root", ProfileVolumeOperation.ActivatePlace, 100), default);

        Assert.Single(volumes.Personal);
        Assert.Empty(volumes.Referral);
    }

    private sealed class Repository(string? parentProfileAddr) : PlaceRepositoryStub
    {
        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<Place?>(Place.Create(
                parentId: 1,
                marketingAddr,
                structureNumber: 0,
                profileAddr,
                profileLogin: "profile",
                index: "profile1",
                placeNumber: 1,
                parentProfileAddr,
                parentProfileLogin: parentProfileAddr,
                parentPlaceNumber: 1,
                mp: "0000000000000001",
                posGroup: 0,
                kind: 0,
                pos: 1,
                filling: 0,
                deep: 2,
                isActive: true,
                createdAt: 1,
                activatedAt: 1));
    }

    private sealed class VolumeRepository : IProfileVolumeRepository
    {
        public List<(string, byte, string, uint)> Personal { get; } = [];
        public List<(string, byte, string, uint)> Referral { get; } = [];

        public Task IncreasePersonalAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            uint amount,
            CancellationToken cancellationToken)
        {
            Personal.Add((marketingAddr, structureNumber, profileAddr, amount));
            return Task.CompletedTask;
        }

        public Task IncreaseReferralAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            uint amount,
            CancellationToken cancellationToken)
        {
            Referral.Add((marketingAddr, structureNumber, profileAddr, amount));
            return Task.CompletedTask;
        }
    }
}
