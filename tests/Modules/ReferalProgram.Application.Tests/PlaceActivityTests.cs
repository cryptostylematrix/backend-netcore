using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;

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
            activatedAt: null);

        place.Activate(10, setActiveOnActivation: false);

        Assert.Equal(10, place.ActivatedAt);
        Assert.False(place.IsActive);
        Assert.Contains(place.DomainEvents, domainEvent =>
            domainEvent is ProfileVolumeOperationDomainEvent volume
            && volume.Operation == ProfileVolumeOperation.ActivatePlace);
    }

    [Theory]
    [InlineData(true, null, false)]
    [InlineData(false, null, false)]
    [InlineData(true, 10, true)]
    [InlineData(false, 10, true)]
    public void Reset_activity_sets_status_from_activation_date_before_clearing_it(
        bool isActive,
        int? activatedAt,
        bool expectedIsActive)
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
            isActive,
            createdAt: 1,
            activatedAt);

        place.ResetActivity();

        Assert.Equal(expectedIsActive, place.IsActive);
        Assert.Null(place.ActivatedAt);
    }
}
