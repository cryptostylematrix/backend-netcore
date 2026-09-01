using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class PlaceVolumeOperationTests
{
    [Theory]
    [InlineData(ProfileVolumeOperation.BuyFirstPlace)]
    [InlineData(ProfileVolumeOperation.BuyPlace)]
    public void Profile_purchase_records_exactly_one_volume_operation(
        ProfileVolumeOperation operation)
    {
        var place = Place.Buy(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 2,
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
            deep: 2,
            boughtAt: 10,
            volumeOperation: operation);

        var volumeEvent = Assert.Single(
            place.DomainEvents.OfType<ProfileVolumeOperationDomainEvent>());
        Assert.Equal(operation, volumeEvent.Operation);
    }

    [Fact]
    public void System_purchase_records_no_profile_volume_operation()
    {
        var place = Place.BuySystem(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 2,
            index: "system1",
            placeNumber: 1,
            parentProfileAddr: "parent",
            parentProfileLogin: "parent",
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.Purchased,
            pos: 1,
            deep: 2,
            boughtAt: 10);

        Assert.Empty(place.DomainEvents.OfType<ProfileVolumeOperationDomainEvent>());
    }
}
