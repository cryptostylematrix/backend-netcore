using System.Reflection;
using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class StructureCompressionServiceTests
{
    [Fact]
    public async Task Removes_ineligible_places_and_reposts_by_rank_volume_then_activation_date()
    {
        var root = Place(1, "root", active: true, activatedAt: 1, personalVolume: 0);
        SetParentId(root, null);
        var highVolumeLater = Place(2, "high-volume-later", active: true, activatedAt: 30, personalVolume: 20);
        var highVolumeEarlier = Place(3, "high-volume-earlier", active: true, activatedAt: 20, personalVolume: 20);
        var lowerVolumeEarliest = Place(7, "lower-volume-earliest", active: true, activatedAt: 5, personalVolume: 10);
        var lowEarlier = Place(4, "low", active: true, activatedAt: 10, personalVolume: 0);
        var system = Place(5, null, active: true, activatedAt: 5, personalVolume: 100);
        var inactive = Place(6, "inactive", active: false, activatedAt: 2, personalVolume: 100);
        var repository = new Repository(
            [root, highVolumeLater, highVolumeEarlier, lowerVolumeEarliest, lowEarlier, system, inactive]);
        var unitOfWork = new UnitOfWork();
        var service = new StructureCompressionService(
            repository,
            new LockRepository(),
            new StructureQueries(),
            new RankQueries(),
            new PositionAlgorithmConfigurationParser(),
            unitOfWork);

        var error = await service.CompressAsync("marketing", 1, default);

        Assert.Null(error);
        Assert.Equal("0000000000000001", highVolumeEarlier.Mp);
        Assert.Equal("0000000000000002", highVolumeLater.Mp);
        Assert.Equal(highVolumeEarlier.Id, lowerVolumeEarliest.ParentId);
        Assert.Equal("000000000000000100000001", lowerVolumeEarliest.Mp);
        Assert.Equal(highVolumeEarlier.Id, lowEarlier.ParentId);
        Assert.Equal("000000000000000100000002", lowEarlier.Mp);
        Assert.All(
            new[] { root, highVolumeLater, highVolumeEarlier, lowerVolumeEarliest, lowEarlier },
            place => Assert.Equal((byte)0, place.PosGroup));
        Assert.Equal([system.Id, inactive.Id], repository.Removed.Select(place => place.Id));
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(5, root.MatrixFilling);
    }

    [Fact]
    public async Task Switches_from_classic_to_topmost_leftmost_empty_parent_positioning()
    {
        var places = Enumerable.Range(1, 10)
            .Select(id => Place(
                id,
                $"profile-{id}",
                active: true,
                activatedAt: id,
                personalVolume: 0))
            .ToArray();
        SetParentId(places[0], null);
        var repository = new Repository(places);
        var service = new StructureCompressionService(
            repository,
            new LockRepository(),
            new StructureQueries(),
            new RankQueries(),
            new PositionAlgorithmConfigurationParser(),
            new UnitOfWork());

        var error = await service.CompressAsync("marketing", 1, default);

        Assert.Null(error);
        Assert.Equal(places[3].Id, places[7].ParentId);
        Assert.Equal(places[4].Id, places[8].ParentId);
        Assert.Equal(places[5].Id, places[9].ParentId);
    }

    private static Place Place(
        int id,
        string? profile,
        bool active,
        long? activatedAt,
        uint personalVolume)
    {
        var place = ReferalProgram.Core.PlaceAggregate.Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 1,
            profileAddr: profile,
            profileLogin: profile,
            index: profile ?? $"system-{id}",
            placeNumber: 1,
            parentProfileAddr: "root",
            parentProfileLogin: "root",
            parentPlaceNumber: 1,
            mp: $"00000000{id:X8}",
            posGroup: 0,
            kind: 0,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: active,
            createdAt: activatedAt ?? 1,
            activatedAt,
            personalVolume,
            groupVolume: 0);
        typeof(ReferalProgram.Core.PlaceAggregate.Place).GetProperty(nameof(place.Id))!
            .SetValue(place, id);
        return place;
    }

    private static void SetParentId(Place place, int? parentId) =>
        typeof(ReferalProgram.Core.PlaceAggregate.Place)
            .GetProperty(nameof(ReferalProgram.Core.PlaceAggregate.Place.ParentId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(place, parentId);

    private sealed class StructureQueries : IStructureQueries
    {
        private static readonly JsonElement Algorithm = JsonDocument.Parse("""
            {"v":1,"root":"owner","groups":[{"id":7,"algo":"radar","weight":3}],"relation":"absolute"}
            """).RootElement.Clone();

        public Task<StructureResponse?> GetStructureAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<StructureResponse?>(new StructureResponse
            {
                MarketingAddr = marketingAddr,
                StructureNumber = structureNumber,
                Width = 2,
                Height = 2,
                PosAlgo = Algorithm
            });
    }

    private sealed class RankQueries : IStructureRankQueries
    {
        public Task<IReadOnlyCollection<StructureRankResponse>> GetAllAsync(
            string marketingAddr,
            byte structureNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<StructureRankResponse>>
            ([
                new StructureRankResponse
                {
                    MarketingAddr = marketingAddr,
                    StructureNumber = structureNumber,
                    Name = "high",
                    RequiredActiveReferralPlaces = 5
                }
            ]);
    }

    private sealed class UnitOfWork : IProgramUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
        public void Dispose() { }
    }

    private sealed class LockRepository : IPositionLockRepository
    {
        public Task<IReadOnlyList<PositionLock>> GetStructureLocksAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PositionLock>>([]);
        public Task<PositionLock?> GetAsync(string marketingAddr, byte structureNumber, string placeProfileAddr, uint placeNumber, string profileAddr, uint lockedPos, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PositionLock>> GetForPlaceAsync(string marketingAddr, byte structureNumber, string placeProfileAddr, uint placeNumber, string profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(PositionLock positionLock) => throw new NotSupportedException();
        public void Remove(PositionLock positionLock) => throw new NotSupportedException();
        public void RemoveRange(IEnumerable<PositionLock> positionLocks) { }
    }

    private sealed class Repository(IReadOnlyList<Place> places) : IPlaceRepository
    {
        public IReadOnlyList<Place> Removed { get; private set; } = [];
        public Task<IReadOnlyList<Place>> GetStructurePlacesAsync(
            string marketingAddr, byte structureNumber, CancellationToken cancellationToken) =>
            Task.FromResult(places);
        public Task<IReadOnlyDictionary<string, string?>> GetInvitersAsync(
            string marketingAddr, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>());
        public Task RemoveRangeAsync(
            IReadOnlyCollection<Place> removed, CancellationToken cancellationToken)
        {
            Removed = removed.ToArray();
            return Task.CompletedTask;
        }
        public Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Place?> GetAsync(string marketingAddr, byte structureNumber, string? profileAddr, uint placeNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<uint> GetNextPlaceNumberAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<long> CountAtDepthAsync(string marketingAddr, byte structureNumber, string mpPrefix, uint depth, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<long> CountCloneChildrenAsync(int parentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task IncrementMatrixFillingForAncestorsAsync(int parentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(Place place) => throw new NotSupportedException();
    }
}
