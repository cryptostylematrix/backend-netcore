using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class PlaceActivatedDomainEventHandlerTests
{
    [Theory]
    [InlineData(null, "Main", false, false, false)]
    [InlineData("structure", "Main", true, false, false)]
    [InlineData("group", " Main ", true, true, false)]
    [InlineData("group", null, true, false, false)]
    [InlineData("group", "  ", true, false, false)]
    [InlineData("program", null, true, true, true)]
    public async Task Synchronizes_only_selected_structures_of_same_profile_and_program(
        string? sync, string? group, bool same, bool grouped, bool other)
    {
        var source = CreatePlace(1, 1);
        var sibling = CreatePlace(1, 2);
        var groupPlace = CreatePlace(2, 1);
        var otherPlace = CreatePlace(3, 1);
        var otherProfile = CreatePlace(2, 1, profile: "other");
        var otherProgram = CreatePlace(2, 1, marketing: "other");
        var systemPlace = CreatePlace(2, 1, profile: null);
        var repository = new Repository(source, sibling, groupPlace, otherPlace,
            otherProfile, otherProgram, systemPlace);
        var structures = new Structures(
            Structure(1, group, sync), Structure(2, "Main", "program"),
            Structure(3, "main", null));
        source.Activate(100, false);

        await new PlaceActivatedDomainEventHandler(repository, structures, structures)
            .Handle(Assert.Single(source.DomainEvents.OfType<PlaceActivatedDomainEvent>()), default);

        Assert.Equal(100, source.ActivatedAt);
        Assert.False(source.IsActive); // Never reapply destination settings to the source.
        Assert.Equal(same ? 100L : null, sibling.ActivatedAt);
        Assert.Equal(grouped ? 100L : null, groupPlace.ActivatedAt);
        Assert.Equal(other ? 100L : null, otherPlace.ActivatedAt);
        Assert.Equal(same, sibling.IsActive);
        Assert.Equal(grouped, groupPlace.IsActive);
        Assert.Equal(other, otherPlace.IsActive);
        foreach (var excluded in new[] { otherProfile, otherProgram, systemPlace })
        {
            Assert.Null(excluded.ActivatedAt);
            Assert.False(excluded.IsActive);
        }
        foreach (var destination in new[] { sibling, groupPlace, otherPlace })
            Assert.Empty(destination.DomainEvents);
        Assert.Single(source.DomainEvents.OfType<ProfileVolumeOperationDomainEvent>());
        Assert.Equal(sync is null ? 0 : 1, repository.ReadCount);
    }

    [Theory]
    [InlineData(null, true, false, 100L, true)]
    [InlineData(50L, true, false, 100L, true)]
    [InlineData(100L, true, false, 100L, true)]
    [InlineData(200L, true, false, 200L, true)]
    [InlineData(50L, false, false, 100L, false)]
    [InlineData(50L, false, true, 100L, true)]
    [InlineData(200L, false, false, 200L, false)]
    public async Task Uses_destination_flag_setting_and_never_moves_date_backwards(
        long? previous, bool setActive, bool wasActive, long expectedDate, bool expectedActive)
    {
        var target = CreatePlace(2, 1, activatedAt: previous, active: wasActive);
        var structures = new Structures(Structure(1, null, "program"),
            Structure(2, null, null, setActive));
        var handler = new PlaceActivatedDomainEventHandler(new Repository(target), structures, structures);
        var notification = new PlaceActivatedDomainEvent("marketing", 1, "profile", 1, 100);

        await handler.Handle(notification, default);
        await handler.Handle(notification, default);

        Assert.Equal(expectedDate, target.ActivatedAt);
        Assert.Equal(expectedActive, target.IsActive);
        Assert.Empty(target.DomainEvents);
    }

    [Fact]
    public async Task Destination_without_activity_gets_date_but_keeps_flag()
    {
        var target = CreatePlace(0, 1);
        // Profiled roots are included: there is deliberately no parent filter.
        typeof(Place).GetProperty(nameof(Place.ParentId))!.SetValue(target, null);
        var structures = new Structures(Structure(1, null, "program"),
            new StructureResponse { MarketingAddr = "marketing", StructureNumber = 0 });

        await new PlaceActivatedDomainEventHandler(new Repository(target), structures, structures)
            .Handle(new PlaceActivatedDomainEvent("marketing", 1, "profile", 1, 100), default);

        Assert.Equal(100, target.ActivatedAt);
        Assert.False(target.IsActive);
        Assert.Empty(target.DomainEvents);
    }

    [Fact]
    public void Already_activated_source_emits_no_events_and_is_rejected()
    {
        var source = CreatePlace(1, 1, activatedAt: 50);
        Assert.Throws<InvalidOperationException>(() => source.Activate(100, true));
        Assert.Equal(50, source.ActivatedAt);
        Assert.Empty(source.DomainEvents);
    }

    private static StructureResponse Structure(byte number, string? group, string? sync,
        bool setActive = true) => new()
    {
        MarketingAddr = "marketing",
        StructureNumber = number,
        Group = group,
        Activity = JsonSerializer.SerializeToElement(new
        {
            activation_sync = sync,
            set_active_on_activation = setActive
        })
    };

    private static Place CreatePlace(byte structure, uint number, string? profile = "profile",
        string marketing = "marketing", long? activatedAt = null, bool active = false)
    {
        var place = ReferalProgram.Core.PlaceAggregate.Place.Create(
            parentId: 1, marketingAddr: marketing, structureNumber: structure,
            profileAddr: profile, profileLogin: profile, index: "place" + number,
            placeNumber: number, parentProfileAddr: "parent", parentProfileLogin: "parent",
            parentPlaceNumber: 1, mp: "0000000000000001", posGroup: 0,
            kind: PlaceKinds.Purchased, pos: 1, filling: 0, deep: 2,
            isActive: active, createdAt: 1, activatedAt: activatedAt);
        place.ClearDomainEvents();
        return place;
    }

    private sealed class Structures(params StructureResponse[] values)
        : IStructureQueries, IProgramStructureListQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber,
            CancellationToken cancellationToken) => Task.FromResult(values.SingleOrDefault(
                structure => structure.MarketingAddr == marketingAddr
                    && structure.StructureNumber == structureNumber));

        public Task<IReadOnlyList<StructureResponse>> GetAsync(string marketingAddress,
            CancellationToken ct) => Task.FromResult<IReadOnlyList<StructureResponse>>(values);
    }

    private sealed class Repository(params Place[] places) : PlaceRepositoryStub
    {
        public int ReadCount { get; private set; }

        public override Task<IReadOnlyList<Place>> GetProfilePlacesAsync(string marketingAddr,
            string profileAddr, byte[] structureNumbers, CancellationToken cancellationToken)
        {
            ReadCount++;
            Assert.Equal("marketing", marketingAddr);
            Assert.Equal("profile", profileAddr);
            return Task.FromResult<IReadOnlyList<Place>>(places.Where(place =>
                place.MarketingAddr == marketingAddr && place.ProfileAddr == profileAddr
                && structureNumbers.Contains(place.StructureNumber)).ToArray());
        }
    }
}
