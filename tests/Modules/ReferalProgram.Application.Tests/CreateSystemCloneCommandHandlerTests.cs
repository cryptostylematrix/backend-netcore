using Common.Domain;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Application.Policies;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class CreateSystemCloneCommandHandlerTests
{
    [Theory]
    [InlineData(PositionOperation.CreateClone)]
    [InlineData(PositionOperation.CreateReinvest)]
    public async Task Persists_kind_calculated_for_trimmed_classic(
        PositionOperation operation)
    {
        var repository = new Repository(Parent())
        {
            ExistingCloneChildren = 1
        };
        var unitOfWork = new UnitOfWork();
        var handler = new CreateSystemCloneCommandHandler(
            repository,
            new Structures(),
            new RelativeResolver(),
            new NextPosition(operation),
            new ClonePlaceKindPolicy(repository),
            new SourceResolver(),
            unitOfWork);

        var result = await handler.Handle(
            new CreateSystemCloneCommand(
                MarketingAddr: "marketing",
                StructureNumber: 2,
                SourceStructureNumber: 1,
                SourceProfileAddr: "source-profile",
                SourcePlaceNumber: 1,
                RelativeLevel: 1,
                TaskKey: 10,
                QueryId: 20,
                Operation: operation),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedPlace);
        Assert.Equal(PlaceKinds.TerminalClone, repository.AddedPlace.Kind);
        Assert.Equal(repository.Parent.Id, repository.CountedParentId);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    private static Place Parent() => Place.Create(
        parentId: 1,
        marketingAddr: "marketing",
        structureNumber: 2,
        profileAddr: "parent-profile",
        profileLogin: "parent",
        index: "parent1",
        placeNumber: 1,
        parentProfileAddr: "ancestor-profile",
        parentProfileLogin: "ancestor",
        parentPlaceNumber: 1,
        mp: "00000000",
        posGroup: 0,
        kind: PlaceKinds.Purchased,
        pos: 1,
        filling: 0,
        deep: 1,
        isActive: true,
        createdAt: 1,
        activatedAt: 1,
        personalVolume: 0,
        groupVolume: 0);

    private sealed class Repository(Place parent) : PlaceRepositoryStub
    {
        public Place Parent { get; } = parent;
        public long ExistingCloneChildren { get; init; }
        public int? CountedParentId { get; private set; }
        public Place? AddedPlace { get; private set; }

        public override Task<Place?> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) => Task.FromResult<Place?>(Parent);

        public override Task<uint> GetNextPlaceNumberAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            CancellationToken cancellationToken) => Task.FromResult<uint>(2);

        public override Task<long> CountCloneChildrenAsync(
            int parentId,
            CancellationToken cancellationToken)
        {
            CountedParentId = parentId;
            return Task.FromResult(ExistingCloneChildren);
        }

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
                Width = 2,
                Height = 1
            });
    }

    private sealed class RelativeResolver : IRelativePlaceResolver
    {
        public Task<RelativePlaceResolution?> ResolveAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            ushort level,
            CancellationToken cancellationToken)
        {
            var place = new PlaceResponse
            {
                MarketingAddr = marketingAddr,
                StructNumber = structureNumber,
                ProfileAddr = "clone-profile",
                ProfileLogin = "clone",
                PlaceNumber = 1,
                Mp = "00000000"
            };
            return Task.FromResult<RelativePlaceResolution?>(new(place, place));
        }
    }

    private sealed class NextPosition(PositionOperation expectedOperation) : INextPosService
    {
        public Task<PositionSelection?> ResolveSelectionAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            PositionOperation? operation,
            CancellationToken ct)
        {
            Assert.Equal(expectedOperation, operation);
            return Task.FromResult<PositionSelection?>(new PositionSelection(
                "trimmed_classic",
                new PositionAlgorithmStrategyContext(
                    marketingAddr,
                    structureNumber,
                    Width: 2,
                    Root: new PlaceResponse
                    {
                        MarketingAddr = marketingAddr,
                        StructNumber = structureNumber,
                        ProfileAddr = profileAddr,
                        ProfileLogin = "clone",
                        PlaceNumber = 1,
                        Mp = "00000000"
                    },
                    PosGroup: 0,
                    ProfiledPlacesPrioritized: true,
                    DepthSpread: 1,
                    RootProfileLockMps: [],
                    CutFactor: 2)));
        }

        public Task<NextPosResponse?> FindNextAsync(
            PositionSelection selection,
            CancellationToken ct) => Task.FromResult<NextPosResponse?>(new NextPosResponse
            {
                ProfileAddr = "parent-profile",
                PlaceNumber = 1,
                Pos = 1,
                PosGroup = 0,
                Mp = "0000000000000001"
            });

        public Task<NextPosResponse?> GetNextPosAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            PositionOperation? operation,
            CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class SourceResolver : ISourcePlaceResolver
    {
        public Task<SourcePlaceResolution?> ResolveAsync(
            Place place,
            byte structureHeight,
            CancellationToken cancellationToken) =>
            Task.FromResult<SourcePlaceResolution?>(new SourcePlaceResolution(
                Code: 1,
                SourcePlace: place));
    }

    private sealed class UnitOfWork : IProgramUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }

        public void Dispose() { }
    }
}
