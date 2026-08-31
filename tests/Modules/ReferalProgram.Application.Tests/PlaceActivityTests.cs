using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class PlaceActivityTests
{
    [Fact]
    public void Activation_always_sets_date_but_can_leave_place_inactive()
    {
        var place = Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 1,
            profileAddr: "profile",
            profileLogin: "profile",
            index: "profile1",
            placeNumber: 1,
            parentProfileAddr: "parent",
            parentProfileLogin: "parent",
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: false,
            createdAt: 1,
            activatedAt: null,
            personalVolume: 0,
            groupVolume: 0);

        place.Activate(10, setActiveOnActivation: false);

        Assert.Equal(10, place.ActivatedAt);
        Assert.False(place.IsActive);
        Assert.Contains(place.DomainEvents, domainEvent =>
            domainEvent is PlaceActivatedDomainEvent activated
            && activated.StructureNumber == 1
            && activated.ProfileAddr == "profile"
            && activated.PlaceNumber == 1);
    }
}
