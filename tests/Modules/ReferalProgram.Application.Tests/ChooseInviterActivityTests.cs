using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Invites;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ChooseInviterActivityTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var active in new[] { false, true })
        foreach (var hasPlaces in new[] { false, true })
        foreach (var without in new[] { false, true })
        foreach (var with in new[] { false, true })
            yield return [active, hasPlaces, without, with];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Invitation_uses_the_matching_inactive_rule_only(
        bool active, bool hasPlaces, bool without, bool with)
    {
        var queries = new Queries(active, hasPlaces);
        var repository = new Repository();
        var uow = new UnitOfWork();
        var activity = JsonSerializer.SerializeToElement(new
        {
            type = "invite",
            when_inactive = new { allow_inviting_without_places = without, allow_inviting_with_places = with }
        });
        var handler = new ChooseInviterCommandHandler(queries, repository,
            new Structures(activity), new Source(), uow);
        var result = await handler.Handle(Command(), default);
        var expected = active || (hasPlaces ? with : without);
        Assert.True(expected == result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(expected ? 1 : 0, uow.Saves);
        Assert.Equal(!active && (with || without) ? 1 : 0, queries.PresenceChecks);
        if (expected)
        {
            var created = Assert.Single(repository.Added);
            Assert.False(created.IsActive);
            Assert.Null(created.ActivatedAt);
            Assert.Equal("inviter", created.ParentProfileAddr);
            Assert.Equal("new-profile", created.ProfileAddr);
            Assert.Equal((byte)0, created.StructureNumber);
            Assert.Equal((uint)1, created.PlaceNumber);
        }
        else Assert.Empty(repository.Added);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("{\"set_active_on_activation\":true}")]
    public async Task Legacy_or_missing_activity_keeps_inactive_inviter_rejected(string? json)
    {
        var repository = new Repository();
        var uow = new UnitOfWork();
        var handler = new ChooseInviterCommandHandler(new Queries(false, true), repository,
            new Structures(json is null ? null : JsonSerializer.Deserialize<JsonElement>(json)), new Source(), uow);
        Assert.False((await handler.Handle(Command(), default)).IsSuccess);
        Assert.Empty(repository.Added);
        Assert.Equal(0, uow.Saves);
    }

    [Theory]
    [InlineData("{\"type\":\"marketing\"}")]
    [InlineData("{\"type\":\"invite\",\"when_inactive\":{\"allow_inviting_with_places\":\"true\"}}")]
    public async Task Invalid_config_does_not_create_or_save_an_invite(string json)
    {
        var repository = new Repository();
        var uow = new UnitOfWork();
        var handler = new ChooseInviterCommandHandler(new Queries(false, true), repository,
            new Structures(JsonSerializer.Deserialize<JsonElement>(json)), new Source(), uow);
        Assert.False((await handler.Handle(Command(), default)).IsSuccess);
        Assert.Empty(repository.Added);
        Assert.Equal(0, uow.Saves);
    }

    private static ChooseInviterCommand Command() => new("marketing", "inviter", "new-profile", 1, 1, null, "new-login");

    private sealed class Queries(bool active, bool hasPlaces) : PlaceQueriesStub
    {
        public int PresenceChecks { get; private set; }
        public override Task<PlaceResponse?> GetPlaceAsync(string marketingAddr, byte structureNumber,
            string? profileAddr, uint placeNumber, CancellationToken cancellationToken)
        {
            Assert.Equal("marketing", marketingAddr);
            Assert.Equal((byte)0, structureNumber);
            Assert.Equal((uint)1, placeNumber);
            return Task.FromResult<PlaceResponse?>(profileAddr == "inviter" ? new PlaceResponse
            {
                Id = 1, MarketingAddr = marketingAddr, StructNumber = 0,
                ProfileAddr = "inviter", ProfileLogin = "inviter", PlaceNumber = 1,
                IsActive = active, Mp = "00000001", Deep = 1
            } : null);
        }
        public override Task<bool> HasProfilePlacesOutsideInviteStructureAsync(string marketingAddr,
            string profileAddr, CancellationToken cancellationToken)
        {
            Assert.Equal("marketing", marketingAddr);
            Assert.Equal("inviter", profileAddr);
            PresenceChecks++;
            return Task.FromResult(hasPlaces);
        }
    }

    private sealed class Structures(JsonElement? activity) : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber,
            CancellationToken cancellationToken) => Task.FromResult<StructureResponse?>(new()
            { StructureNumber = 0, Activity = activity });
    }

    private sealed class Repository : PlaceRepositoryStub
    {
        public List<Place> Added { get; } = [];
        public override void Add(Place place) => Added.Add(place);
        public override Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<Place?>(Place.Create(parentId: 0, marketingAddr: "marketing", structureNumber: 0,
                profileAddr: "inviter", profileLogin: "inviter", index: "inviter1", placeNumber: 1,
                parentProfileAddr: null, parentProfileLogin: null, parentPlaceNumber: 0,
                mp: "00000001", posGroup: 0, kind: 0, pos: 1, filling: 0, deep: 1,
                isActive: false, createdAt: 1, activatedAt: null));
    }

    private sealed class Source : ISourcePlaceResolver
    {
        public Task<SourcePlaceResolution?> ResolveAsync(Place place, byte structureHeight,
            CancellationToken cancellationToken) => Task.FromResult<SourcePlaceResolution?>(new(0, place));
    }

    private sealed class UnitOfWork : IProgramUnitOfWork
    {
        public int Saves { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(++Saves);
        public void Dispose() { }
    }
}
