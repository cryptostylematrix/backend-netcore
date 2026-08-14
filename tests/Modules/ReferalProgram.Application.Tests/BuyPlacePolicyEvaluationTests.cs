using System.Text.Json;
using Common.Dto;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class BuyPlacePolicyEvaluationTests
{
    [Fact]
    public async Task Denies_when_structure_does_not_exist()
    {
        var policy = Policy(structure: null);

        var result = await policy.EvaluateAsync("marketing", 4, "profile", null, default);

        Assert.False(result.CanBuy);
        Assert.Equal("structure_not_found", result.Reason);
    }

    [Fact]
    public async Task Denies_when_profile_reached_the_configured_limit()
    {
        var policy = Policy(
            Structure("owner", maxPlaces: 2),
            placesCount: 2);

        var result = await policy.EvaluateAsync("marketing", 4, "profile", null, default);

        Assert.False(result.CanBuy);
        Assert.Equal("max_places_reached", result.Reason);
    }

    [Fact]
    public async Task Owner_root_selects_buy_first_for_profiles_without_places()
    {
        var parent = Place("ROOT", "parent", 1, filling: 1);
        var next = new NextPosResponse
        {
            Mp = "ROOT00000002",
            ProfileAddr = parent.ProfileAddr,
            PlaceNumber = parent.PlaceNumber,
            Pos = 2,
            PosGroup = 1
        };
        var policy = Policy(
            Structure("owner"),
            placesCount: 0,
            nextPosition: next,
            viewerRoot: parent,
            places: [parent],
            tags: new HashSet<uint> {
                ProgramCommandTags.BuyPlace,
                ProgramCommandTags.BuyFirstPlace });

        var result = await policy.EvaluateAsync("marketing", 4, "profile", null, default);

        Assert.True(result.CanBuy);
        Assert.Equal(BuyPlaceKind.First, result.Kind);
        Assert.Equal(ProgramCommandTags.BuyFirstPlace, result.CommandTag);
        Assert.False(result.IncludePosition);
        Assert.True(result.RequireNextPosition);
        Assert.Same(next, result.Position);
    }

    [Fact]
    public async Task Profile_root_accepts_an_explicit_unlocked_position_in_viewer_subtree()
    {
        var root = Place("ROOT", "viewer", 1, filling: 1);
        var parent = Place("ROOT00000001", "parent", 2, filling: 0);
        var calculated = new NextPosResponse
        {
            Mp = "ROOT00000009",
            ProfileAddr = root.ProfileAddr,
            PlaceNumber = root.PlaceNumber,
            Pos = 9
        };
        var policy = Policy(
            Structure("profile", width: 3),
            placesCount: 1,
            nextPosition: calculated,
            viewerRoot: root,
            places: [root, parent],
            tags: new HashSet<uint> { ProgramCommandTags.BuyPlace });

        var result = await policy.EvaluateAsync(
            "marketing",
            4,
            "viewer",
            new RequestedPosition(4, "parent", 2, 1),
            default);

        Assert.True(result.CanBuy);
        Assert.Equal(BuyPlaceKind.Regular, result.Kind);
        Assert.True(result.IncludePosition);
        Assert.False(result.RequireNextPosition);
        Assert.Equal("ROOT0000000100000001", result.Position!.Mp);
        Assert.Equal("parent", result.Position.ProfileAddr);
    }

    [Fact]
    public async Task Profile_root_rejects_an_explicit_position_inside_a_lock()
    {
        var root = Place("ROOT", "viewer", 1, filling: 1);
        var parent = Place("ROOT00000001", "parent", 2, filling: 0);
        var policy = Policy(
            Structure("profile", width: 3),
            nextPosition: new NextPosResponse
            {
                Mp = "ROOT00000009",
                ProfileAddr = root.ProfileAddr,
                PlaceNumber = 1,
                Pos = 1
            },
            viewerRoot: root,
            places: [root, parent],
            lockMps: ["ROOT0000000100000001"]);

        var result = await policy.EvaluateAsync(
            "marketing", 4, "viewer",
            new RequestedPosition(4, "parent", 2, 1), default);

        Assert.False(result.CanBuy);
        Assert.Equal("position_is_locked", result.Reason);
    }

    private static BuyPlacePolicy Policy(
        StructureResponse? structure,
        long placesCount = 0,
        NextPosResponse? nextPosition = null,
        PlaceResponse? viewerRoot = null,
        IReadOnlyCollection<PlaceResponse>? places = null,
        IReadOnlySet<uint>? tags = null,
        string[]? lockMps = null)
    {
        var placeQueries = new Queries(placesCount, places ?? []);
        return new BuyPlacePolicy(
            new StructureQueries(structure),
            placeQueries,
            new Locks(lockMps ?? []),
            new NextPosition(nextPosition),
            new RootResolver(viewerRoot),
            new PositionAlgorithmConfigurationParser(),
            new Commands(tags ?? new HashSet<uint>()));
    }

    private static StructureResponse Structure(
        string root,
        int maxPlaces = 0,
        byte width = 3)
    {
        using var document = JsonDocument.Parse($$"""
            {
              "v": 1,
              "root": "{{root}}",
              "relation": "relative",
              "groups": [{ "id": 0, "algo": "chess", "weight": 1 }]
            }
            """);

        return new StructureResponse
        {
            MarketingAddr = "marketing",
            StructureNumber = 4,
            MaxPlacesPerProfile = maxPlaces,
            Width = width,
            PosAlgo = document.RootElement.Clone()
        };
    }

    private static PlaceResponse Place(
        string mp,
        string? profile,
        uint placeNumber,
        uint filling) => new()
    {
        MarketingAddr = "marketing",
        StructNumber = 4,
        ProfileAddr = profile,
        ProfileLogin = profile,
        PlaceNumber = placeNumber,
        Mp = mp,
        Filling = filling,
        IsActive = true
    };

    private sealed class StructureQueries(StructureResponse? structure) : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => Task.FromResult(structure);
    }

    private sealed class Queries(
        long count,
        IReadOnlyCollection<PlaceResponse> places) : PlaceQueriesStub
    {
        public override Task<long> GetPlacesCountAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => Task.FromResult(count);

        public override Task<PlaceResponse?> GetPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, uint placeNumber, CancellationToken cancellationToken) =>
            Task.FromResult(places.SingleOrDefault(place =>
                place.MarketingAddr == marketingAddr
                && place.StructNumber == structureNumber
                && place.ProfileAddr == profileAddr
                && place.PlaceNumber == placeNumber));

    }

    private sealed class Locks(string[] mps) : ILockQueries
    {
        public Task<string[]> GetAllLockMpsAsync(string marketingAddr, byte structNumber, string? profileAddr, CancellationToken ct) => Task.FromResult(mps);
        public Task<Paginated<LockResponse>> GetLocksAsync(string marketingAddr, byte structNumber, string profileAddr, int page, int pageSize, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class NextPosition(NextPosResponse? result) : INextPosService
    {
        public Task<NextPosResponse?> GetNextPosAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken ct) => Task.FromResult(result);
    }

    private sealed class RootResolver(PlaceResponse? result) : IPositionRootResolver
    {
        public Task<PlaceResponse?> ResolveAsync(string strategyName, string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class Commands(IReadOnlySet<uint> tags) : IProgramCommandQueries
    {
        public Task<IReadOnlySet<uint>> GetAvailableCommandTagsAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => Task.FromResult(tags);
    }
}
