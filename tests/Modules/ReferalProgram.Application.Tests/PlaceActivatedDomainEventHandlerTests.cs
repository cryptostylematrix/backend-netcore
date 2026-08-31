using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class PlaceActivatedDomainEventHandlerTests
{
    [Fact]
    public async Task Increments_curator_first_place_personal_volume()
    {
        var repository = new Repository(includeCuratorPlace: true);
        var handler = new PlaceActivatedDomainEventHandler(repository);

        await handler.Handle(
            new PlaceActivatedDomainEvent(
                "marketing",
                structureNumber: 2,
                profileAddr: "referral",
                placeNumber: 1,
                activatedAt: 10),
            default);

        Assert.Equal<uint>(1, repository.CuratorPlace!.PersonalVolume);
    }

    [Fact]
    public async Task Succeeds_when_curator_has_no_first_place_in_structure()
    {
        var repository = new Repository(includeCuratorPlace: false);
        var handler = new PlaceActivatedDomainEventHandler(repository);

        await handler.Handle(
            new PlaceActivatedDomainEvent(
                "marketing",
                structureNumber: 2,
                profileAddr: "referral",
                placeNumber: 1,
                activatedAt: 10),
            default);
    }

    private sealed class Repository(bool includeCuratorPlace) : PlaceRepositoryStub
    {
        public Place Invite { get; } = CreatePlace(
            structureNumber: 0,
            profileAddr: "referral",
            profileLogin: "referral",
            parentProfileAddr: "curator");

        public Place? CuratorPlace { get; } = includeCuratorPlace
            ? CreatePlace(
                structureNumber: 2,
                profileAddr: "curator",
                profileLogin: "curator",
                parentProfileAddr: "root")
            : null;

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken)
        {
            Place? place = (structureNumber, profileAddr, placeNumber) switch
            {
                (0, "referral", 1) => Invite,
                (2, "curator", 1) => CuratorPlace,
                _ => null
            };
            return Task.FromResult(place);
        }
    }

    private static Place CreatePlace(
        byte structureNumber,
        string profileAddr,
        string profileLogin,
        string parentProfileAddr) => Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber,
            profileAddr,
            profileLogin,
            index: profileLogin + "1",
            placeNumber: 1,
            parentProfileAddr,
            parentProfileLogin: parentProfileAddr,
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: true,
            createdAt: 1,
            activatedAt: 1,
            personalVolume: 0,
            groupVolume: 0);
}
