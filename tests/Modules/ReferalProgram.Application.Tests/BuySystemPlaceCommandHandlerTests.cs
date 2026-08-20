using Common.Domain;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class BuySystemPlaceCommandHandlerTests
{
    [Fact]
    public async Task Classic_honors_requested_position_without_profile_subtree_check()
    {
        var repository = new Repository(Parent("OTHER", filling: 1));
        var nextPosService = new NextPosition(
            Selection("classic", rootMp: "ROOT"),
            calculated: null);
        var handler = Handler(repository, nextPosService);

        var result = await handler.Handle(
            Command(new ChildPosition(
                new BuyPlaceRef(2, "parent", 1),
                Position: 2)),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedPlace);
        Assert.Equal("OTHER00000002", repository.AddedPlace.Mp);
        Assert.Equal((uint)2, repository.AddedPlace.Pos);
        Assert.False(repository.AddedPlace.Mp.StartsWith("ROOT", StringComparison.Ordinal));
        Assert.Equal(0, nextPosService.FindNextCallCount);
        Assert.Equal(PositionOperation.BuySystemPlace, nextPosService.Operation);
    }

    [Fact]
    public async Task Classic_rejects_a_requested_locked_position()
    {
        var repository = new Repository(Parent("OTHER", filling: 1));
        var handler = Handler(
            repository,
            new NextPosition(
                Selection(
                    "classic",
                    rootMp: "ROOT",
                    lockMps: ["OTHER00000002"]),
                calculated: null));

        var result = await handler.Handle(
            Command(new ChildPosition(
                new BuyPlaceRef(2, "parent", 1),
                Position: 2)),
            default);

        Assert.False(result.IsSuccess);
        Assert.Null(repository.AddedPlace);
        Assert.Contains("locked", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Radar_ignores_requested_position_and_uses_calculated_candidate()
    {
        var calculatedParent = Parent("CALCULATED", filling: 0);
        var repository = new Repository(calculatedParent);
        var nextPosService = new NextPosition(
            Selection("radar", rootMp: "ROOT"),
            new NextPosResponse
            {
                ProfileAddr = "parent",
                PlaceNumber = 1,
                Pos = 1,
                Mp = "CALCULATED00000001",
                PosGroup = 2
            });
        var handler = Handler(repository, nextPosService);

        var result = await handler.Handle(
            Command(new ChildPosition(
                new BuyPlaceRef(99, "missing", 999),
                Position: 999)),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("CALCULATED00000001", repository.AddedPlace?.Mp);
        Assert.Equal(1, nextPosService.FindNextCallCount);
    }

    private static BuySystemPlaceCommandHandler Handler(
        Repository repository,
        INextPosService nextPosService) => new(
            repository,
            new Structures(),
            nextPosService,
            new SourceResolver(),
            new UnitOfWork());

    private static BuySystemPlaceCommand Command(ChildPosition? position) => new(
        MarketingAddr: "marketing",
        StructureNumber: 2,
        TaskKey: 10,
        QueryId: 20,
        SourceAddr: "wallet",
        ChildPosition: position);

    private static PositionSelection Selection(
        string algorithm,
        string rootMp,
        string[]? lockMps = null) => new(
        algorithm,
        new PositionAlgorithmStrategyContext(
            "marketing",
            2,
            3,
            new PlaceResponse
            {
                MarketingAddr = "marketing",
                StructNumber = 2,
                ProfileAddr = "root-profile",
                ProfileLogin = "root",
                PlaceNumber = 1,
                Mp = rootMp
            },
            PosGroup: 2,
            ProfiledPlacesPrioritized: true,
            DepthSpread: 1,
            RootProfileLockMps: lockMps ?? []));

    private static Place Parent(string mp, uint filling) => Place.Create(
        parentId: 1,
        marketingAddr: "marketing",
        structureNumber: 2,
        profileAddr: "parent",
        profileLogin: "parent",
        index: "parent1",
        placeNumber: 1,
        parentProfileAddr: "ancestor",
        parentProfileLogin: "ancestor",
        parentPlaceNumber: 1,
        mp,
        posGroup: 0,
        kind: 0,
        pos: 1,
        filling,
        deep: 1,
        isActive: true,
        createdAt: 1,
        activatedAt: 1,
        personalVolume: 0,
        groupVolume: 0,
        taskKey: 0,
        taskQueryId: 0,
        taskSourceAddr: null);

    private sealed class Repository(Place parent) : PlaceRepositoryStub
    {
        public Place? AddedPlace { get; private set; }

        public override Task<Place?> GetByTaskKeyAsync(
            string marketingAddr,
            int taskKey,
            CancellationToken cancellationToken) => Task.FromResult<Place?>(null);

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) => Task.FromResult<Place?>(parent);

        public override Task<uint> GetNextPlaceNumberAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            CancellationToken cancellationToken) => Task.FromResult<uint>(1);

        public override void Add(Place place) => AddedPlace = place;
    }

    private sealed class Structures : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<StructureResponse?>(new StructureResponse
            {
                MarketingAddr = marketingAddr,
                StructureNumber = structureNumber,
                Width = 3,
                Height = 0
            });
    }

    private sealed class NextPosition(
        PositionSelection selection,
        NextPosResponse? calculated) : INextPosService
    {
        public int FindNextCallCount { get; private set; }
        public PositionOperation? Operation { get; private set; }

        public Task<PositionSelection?> ResolveSelectionAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            PositionOperation? operation,
            CancellationToken ct)
        {
            Operation = operation;
            return Task.FromResult<PositionSelection?>(selection);
        }

        public Task<NextPosResponse?> FindNextAsync(
            PositionSelection requestedSelection,
            CancellationToken ct)
        {
            FindNextCallCount++;
            return Task.FromResult(calculated);
        }

        public Task<NextPosResponse?> GetNextPosAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            PositionOperation? operation,
            CancellationToken ct) => Task.FromResult(calculated);
    }

    private sealed class SourceResolver : ISourcePlaceResolver
    {
        public Task<SourcePlaceResolution?> ResolveAsync(
            Place place,
            byte structureHeight,
            CancellationToken cancellationToken) =>
            Task.FromResult<SourcePlaceResolution?>(new SourcePlaceResolution(
                0,
                new PlaceResponse
                {
                    MarketingAddr = place.MarketingAddr,
                    StructNumber = place.StructureNumber,
                    ProfileAddr = place.ProfileAddr,
                    ProfileLogin = place.ProfileLogin,
                    PlaceNumber = place.PlaceNumber,
                    Mp = place.Mp
                }));
    }

    private sealed class UnitOfWork : IProgramUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(1);

        public void Dispose()
        {
        }
    }
}
