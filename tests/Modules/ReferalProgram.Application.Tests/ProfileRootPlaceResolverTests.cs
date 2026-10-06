using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Application.Services.RootStrategies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ProfileRootPlaceResolverTests
{
    [Fact]
    public async Task Returns_the_profiles_first_place_when_it_exists()
    {
        var root = Place(10, null, 4, "profile", active: true);
        var resolver = new ProfileRootPlaceResolver(new Queries([root]), new Structures());

        var result = await resolver.ResolveAsync("marketing", 4, "profile", default);

        Assert.Same(root, result);
    }

    [Fact]
    public async Task Walks_to_the_first_active_profiled_inviter()
    {
        var requestedInvite = Place(1, 2, 0, "requested", active: true);
        var systemInviter = Place(2, 3, 0, null, active: true);
        var inactiveInviter = Place(3, 4, 0, "inactive", active: false);
        var activeInviter = Place(4, null, 0, "inviter", active: true);
        var inviterRoot = Place(20, null, 4, "inviter", active: true);
        var resolver = new ProfileRootPlaceResolver(new Queries(
            [requestedInvite, systemInviter, inactiveInviter, activeInviter, inviterRoot]), new Structures());

        var result = await resolver.ResolveAsync("marketing", 4, "requested", default);

        Assert.Same(inviterRoot, result);
    }

    [Fact]
    public async Task Returns_null_for_a_cycle_in_the_inviter_chain()
    {
        var firstInvite = Place(1, 2, 0, "first", active: true);
        var secondInvite = Place(2, 1, 0, "second", active: false);
        var resolver = new ProfileRootPlaceResolver(new Queries([firstInvite, secondInvite]), new Structures());

        var result = await resolver.ResolveAsync("marketing", 4, "first", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Null_profile_uses_the_system_first_place_only()
    {
        var systemRoot = Place(30, null, 4, null, active: true);
        var resolver = new ProfileRootPlaceResolver(new Queries([systemRoot]), new Structures());

        var result = await resolver.ResolveAsync("marketing", 4, "  ", default);

        Assert.Same(systemRoot, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fallback_setting_selects_the_nearest_eligible_inviter(bool allow)
    {
        var requested = Place(1, 2, 0, "requested", true);
        var inactive = Place(2, 3, 0, "inactive", false);
        var active = Place(3, null, 0, "active", true);
        var inactiveRoot = Place(20, null, 4, "inactive", false);
        var activeRoot = Place(30, null, 4, "active", false);
        var structures = new Structures(allow);
        var resolver = new ProfileRootPlaceResolver(
            new Queries([requested, inactive, active, inactiveRoot, activeRoot]), structures);
        var result = await resolver.ResolveAsync("marketing", 4, "requested", default);
        Assert.Same(allow ? inactiveRoot : activeRoot, result);
        Assert.Equal(1, structures.Calls);
    }

    [Fact]
    public async Task Enabled_fallback_continues_past_inviter_without_target_place_and_handles_cycles()
    {
        var requested = Place(1, 2, 0, "requested", true);
        var inactive = Place(2, 3, 0, "inactive", false);
        var ancestor = Place(3, 1, 0, "ancestor", false);
        var structures = new Structures(true);
        var resolver = new ProfileRootPlaceResolver(new Queries([requested, inactive, ancestor]), structures);
        Assert.Null(await resolver.ResolveAsync("marketing", 4, "requested", default));
        Assert.Equal(1, structures.Calls);
    }

    [Fact]
    public async Task Own_inactive_place_does_not_require_invite_configuration()
    {
        var root = Place(10, null, 4, "profile", false);
        var structures = new Structures(true);
        var resolver = new ProfileRootPlaceResolver(new Queries([root]), structures);
        Assert.Same(root, await resolver.ResolveAsync("marketing", 4, "profile", default));
        Assert.Equal(0, structures.Calls);
    }

    [Fact]
    public async Task Fallback_root_uses_its_owners_locks_in_position_selection()
    {
        var root = Place(20, null, 4, "inactive", false);
        var places = new Queries([Place(1, 2, 0, "requested", true), Place(2, null, 0, "inactive", false), root]);
        var locks = new Locks();
        var resolver = new ProfileRootPlaceResolver(places, new Structures(true));
        var service = new NextPosService(new PositionQueries(), new PositionAlgorithmConfigurationParser(),
            new PositionGroupSelector(), new PositionRootResolver([new ProfileRootPlaceStrategy(resolver)]),
            new UnusedAlgorithmResolver(), locks, places);
        var selection = await service.ResolveSelectionAsync("marketing", 4, "requested", null, default);
        Assert.NotNull(selection);
        Assert.Same(root, selection.Context.Root);
        Assert.Equal("inactive", locks.Owner);
        Assert.Equal(["locked-subtree"], selection.Context.RootProfileLockMps);
    }

    private sealed class PositionQueries : INextPositionQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) =>
            Task.FromResult<StructureResponse?>(new StructureResponse
            {
                StructureNumber = 4, Width = 2,
                PosAlgo = JsonSerializer.Deserialize<JsonElement>("""
                    {"v":1,"root":"profile","relation":"relative","groups":[{"id":0,"algo":"classic","weight":1}]}
                    """)
            });
        public Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<byte, long>>(new Dictionary<byte, long>());
    }

    private sealed class Locks : IPositionLockQueries
    {
        public string? Owner { get; private set; }
        public Task<string[]> GetAllLockMpsAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken)
        {
            Owner = profileAddr;
            return Task.FromResult(new[] { "locked-subtree" });
        }
    }

    private sealed class UnusedAlgorithmResolver : IPositionAlgorithmResolver
    {
        public IPositionAlgorithmStrategy Resolve(string name) => throw new NotSupportedException();
    }

    private sealed class Structures(bool? allow = null) : IStructureQueries
    {
        public int Calls { get; private set; }
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken)
        {
            Assert.Equal("marketing", marketingAddr);
            Assert.Equal((byte)0, structureNumber);
            Calls++;
            return Task.FromResult<StructureResponse?>(new StructureResponse
            {
                StructureNumber = 0,
                Activity = allow is null ? null : JsonSerializer.SerializeToElement(new
                {
                    type = "invite", when_inactive = new { allow_as_fallback_root = allow.Value }
                })
            });
        }
    }

    private static PlaceResponse Place(
        int id,
        int? parentId,
        byte structure,
        string? profile,
        bool active) => new()
    {
        Id = id,
        ParentId = parentId,
        MarketingAddr = "marketing",
        StructNumber = structure,
        ProfileAddr = profile,
        ProfileLogin = profile,
        PlaceNumber = 1,
        IsActive = active,
        Mp = id.ToString()
    };

    private sealed class Queries(IEnumerable<PlaceResponse> places) : PlaceQueriesStub
    {
        private readonly IReadOnlyList<PlaceResponse> _places = places.ToArray();

        public override Task<PlaceResponse?> GetPlaceAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(_places.SingleOrDefault(place =>
                place.MarketingAddr == marketingAddr
                && place.StructNumber == structureNumber
                && place.ProfileAddr == profileAddr
                && place.PlaceNumber == placeNumber));

        public override Task<PlaceResponse?> GetPlaceAsync(
            int id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_places.SingleOrDefault(place => place.Id == id));
    }
}
