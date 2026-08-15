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
    public async Task Denies_when_previous_structure_is_required_but_profile_has_no_place_there()
    {
        var policy = Policy(
            Structure("owner", prevRequired: true),
            previousStructurePlacesCount: 0);

        var result = await policy.EvaluateAsync(
            "marketing",
            4,
            "profile",
            null,
            default);

        Assert.False(result.CanBuy);
        Assert.Equal("previous_structure_place_required", result.Reason);
    }

    [Fact]
    public async Task Allows_purchase_evaluation_when_profile_has_a_place_in_required_previous_structure()
    {
        var parent = Place("ROOT", "parent", 1, filling: 1);
        var policy = Policy(
            Structure("owner", prevRequired: true),
            previousStructurePlacesCount: 1,
            nextPosition: new NextPosResponse
            {
                Mp = "ROOT00000002",
                ProfileAddr = parent.ProfileAddr,
                PlaceNumber = parent.PlaceNumber,
                Pos = 2
            },
            viewerRoot: parent,
            places: [parent],
            tags: new HashSet<uint> { ProgramCommandTags.BuyPlace });

        var result = await policy.EvaluateAsync(
            "marketing",
            4,
            "profile",
            null,
            default);

        Assert.True(result.CanBuy);
        Assert.Equal(BuyPlaceKind.Regular, result.Kind);
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
    public async Task Selects_regular_buy_when_profile_has_a_place_in_any_buy_first_structure()
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
            hasPlacesInProgramStructures: true,
            buyFirstPlaceStructureNumbers: new HashSet<byte> { 2, 4 },
            nextPosition: next,
            viewerRoot: parent,
            places: [parent],
            tags: new HashSet<uint>
            {
                ProgramCommandTags.BuyPlace,
                ProgramCommandTags.BuyFirstPlace
            });

        var result = await policy.EvaluateAsync(
            "marketing",
            4,
            "profile",
            null,
            default);

        Assert.True(result.CanBuy);
        Assert.Equal(BuyPlaceKind.Regular, result.Kind);
        Assert.Equal(ProgramCommandTags.BuyPlace, result.CommandTag);
    }

    [Fact]
    public async Task Profile_root_accepts_an_explicit_unlocked_position_in_viewer_subtree()
    {
        var profileRoot = Place("OWN", "viewer", 1, filling: 1);
        var inviterRoot = Place("INVITER", "inviter", 1, filling: 1);
        var parent = Place("OWN00000001", "parent", 2, filling: 0);
        var policy = Policy(
            Structure("profile", width: 3, algorithm: "classic"),
            placesCount: 1,
            nextPosition: null,
            viewerRoot: inviterRoot,
            places: [profileRoot, parent],
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
        Assert.Equal("OWN0000000100000001", result.Position!.Mp);
        Assert.Equal("parent", result.Position.ProfileAddr);
    }

    [Fact]
    public async Task Profile_root_rejects_an_explicit_position_inside_a_lock()
    {
        var root = Place("ROOT", "viewer", 1, filling: 1);
        var parent = Place("ROOT00000001", "parent", 2, filling: 0);
        var policy = Policy(
            Structure("profile", width: 3, algorithm: "classic"),
            nextPosition: null,
            viewerRoot: root,
            places: [root, parent],
            lockMps: ["ROOT0000000100000001"]);

        var result = await policy.EvaluateAsync(
            "marketing", 4, "viewer",
            new RequestedPosition(4, "parent", 2, 1), default);

        Assert.False(result.CanBuy);
        Assert.Equal("position_is_locked", result.Reason);
    }

    [Fact]
    public async Task Radar_ignores_a_supplied_position_and_uses_its_calculated_candidate()
    {
        var root = Place("ROOT", "viewer", 1, filling: 1);
        var calculated = new NextPosResponse
        {
            Mp = "ROOT00000002",
            ProfileAddr = root.ProfileAddr,
            PlaceNumber = root.PlaceNumber,
            Pos = 2
        };
        var policy = Policy(
            Structure("profile", algorithm: "radar"),
            placesCount: 1,
            nextPosition: calculated,
            viewerRoot: root,
            places: [root],
            tags: new HashSet<uint> { ProgramCommandTags.BuyPlace });

        var result = await policy.EvaluateAsync(
            "marketing",
            4,
            "viewer",
            new RequestedPosition(99, "missing", 999, 999),
            default);

        Assert.True(result.CanBuy);
        Assert.False(result.IncludePosition);
        Assert.True(result.RequireNextPosition);
        Assert.Same(calculated, result.Position);
    }

    private static BuyPlacePolicy Policy(
        StructureResponse? structure,
        long placesCount = 0,
        long previousStructurePlacesCount = 0,
        bool hasPlacesInProgramStructures = false,
        IReadOnlySet<byte>? buyFirstPlaceStructureNumbers = null,
        NextPosResponse? nextPosition = null,
        PlaceResponse? viewerRoot = null,
        IReadOnlyCollection<PlaceResponse>? places = null,
        IReadOnlySet<uint>? tags = null,
        string[]? lockMps = null)
    {
        var effectiveTags = tags ?? new HashSet<uint>();
        var effectiveBuyFirstPlaceStructureNumbers =
            buyFirstPlaceStructureNumbers
            ?? (effectiveTags.Contains(ProgramCommandTags.BuyFirstPlace)
                ? new HashSet<byte> { 4 }
                : new HashSet<byte>());
        var placeQueries = new Queries(
            placesCount,
            previousStructurePlacesCount,
            hasPlacesInProgramStructures,
            effectiveBuyFirstPlaceStructureNumbers,
            places ?? []);
        return new BuyPlacePolicy(
            new StructureQueries(structure),
            placeQueries,
            new Locks(lockMps ?? []),
            new NextPosition(
                nextPosition,
                viewerRoot,
                structure is null
                    ? "chess"
                    : structure.PosAlgo
                        .GetProperty("groups")[0]
                        .GetProperty("algo")
                        .GetString()!),
            new Commands(
                effectiveTags,
                effectiveBuyFirstPlaceStructureNumbers));
    }

    private static StructureResponse Structure(
        string root,
        int maxPlaces = 0,
        byte width = 3,
        bool prevRequired = false,
        string algorithm = "chess")
    {
        using var document = JsonDocument.Parse($$"""
            {
              "v": 1,
              "root": "{{root}}",
              "relation": "relative",
              "groups": [{ "id": 0, "algo": "{{algorithm}}", "weight": 1 }]
            }
            """);

        return new StructureResponse
        {
            MarketingAddr = "marketing",
            StructureNumber = 4,
            MaxPlacesPerProfile = maxPlaces,
            Width = width,
            PrevRequired = prevRequired,
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
        long previousStructurePlacesCount,
        bool hasPlacesInProgramStructures,
        IReadOnlySet<byte> expectedStructureNumbers,
        IReadOnlyCollection<PlaceResponse> places) : PlaceQueriesStub
    {
        public override Task<long> GetPlacesCountAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) =>
            Task.FromResult(structureNumber == 3
                ? previousStructurePlacesCount
                : count);

        public override Task<PlaceResponse?> GetFirstPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) =>
            Task.FromResult(places
                .Where(place => place.MarketingAddr == marketingAddr
                    && place.StructNumber == structureNumber
                    && place.ProfileAddr == profileAddr)
                .OrderBy(place => place.PlaceNumber)
                .FirstOrDefault());

        public override Task<bool> HasProfilePlacesInStructuresAsync(string marketingAddr, string profileAddr, IReadOnlyCollection<byte> structureNumbers, CancellationToken cancellationToken)
        {
            Assert.Equal(expectedStructureNumbers.Order(), structureNumbers.Order());
            return Task.FromResult(hasPlacesInProgramStructures);
        }

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

    private sealed class NextPosition(
        NextPosResponse? result,
        PlaceResponse? root,
        string algorithm) : INextPosService
    {
        public Task<PositionSelection?> ResolveSelectionAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken ct) =>
            Task.FromResult(root is null
                ? null
                : new PositionSelection(
                    algorithm,
                    new PositionAlgorithmStrategyContext(
                        marketingAddr,
                        structureNumber,
                        3,
                        root,
                        0,
                        true,
                        1,
                        [])));

        public Task<NextPosResponse?> FindNextAsync(PositionSelection selection, CancellationToken ct) =>
            Task.FromResult(result);

        public Task<NextPosResponse?> GetNextPosAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken ct) => Task.FromResult(result);
    }

    private sealed class Commands(
        IReadOnlySet<uint> tags,
        IReadOnlySet<byte> buyFirstPlaceStructureNumbers) : IProgramCommandQueries
    {
        public Task<ProgramCommandConfiguration> GetConfigurationAsync(string marketingAddr, CancellationToken cancellationToken)
        {
            var commandTagsByStructure = buyFirstPlaceStructureNumbers.ToDictionary(
                structureNumber => structureNumber,
                _ => (IReadOnlySet<uint>)new HashSet<uint>
                {
                    ProgramCommandTags.BuyFirstPlace
                });
            commandTagsByStructure[4] = tags;
            return Task.FromResult(new ProgramCommandConfiguration(
                commandTagsByStructure));
        }
    }
}
