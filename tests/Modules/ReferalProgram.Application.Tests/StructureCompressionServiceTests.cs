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
        var root = Place(1, "root", active: true, activatedAt: 1);
        SetParentId(root, null);
        var highVolumeLater = Place(2, "high-volume-later", active: true, activatedAt: 30);
        var highVolumeEarlier = Place(3, "high-volume-earlier", active: true, activatedAt: 20);
        var lowerVolumeEarliest = Place(7, "lower-volume-earliest", active: true, activatedAt: 5);
        var lowEarlier = Place(4, "low", active: true, activatedAt: 10);
        var system = Place(5, null, active: true, activatedAt: 5);
        var inactive = Place(6, "inactive", active: false, activatedAt: 2);
        var repository = new Repository(
            [root, highVolumeLater, highVolumeEarlier, lowerVolumeEarliest, lowEarlier, system, inactive]);
        var unitOfWork = new UnitOfWork();
        var service = new StructureCompressionService(
            repository,
            new LockRepository(),
            new StructureQueries(),
            new RankQueries(),
            new VolumeQueries(new Dictionary<string, uint>
            {
                ["high-volume-later"] = 20,
                ["high-volume-earlier"] = 20,
                ["lower-volume-earliest"] = 10,
                ["inactive"] = 100
            }),
            new PositionAlgorithmConfigurationParser(),
            unitOfWork, new Queries());

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
                activatedAt: id))
            .ToArray();
        SetParentId(places[0], null);
        var repository = new Repository(places);
        var service = new StructureCompressionService(
            repository,
            new LockRepository(),
            new StructureQueries(),
            new RankQueries(),
            new VolumeQueries(new Dictionary<string, uint>()),
            new PositionAlgorithmConfigurationParser(),
            new UnitOfWork(), new Queries());

        var error = await service.CompressAsync("marketing", 1, default);

        Assert.Null(error);
        Assert.Equal(places[3].Id, places[7].ParentId);
        Assert.Equal(places[4].Id, places[8].ParentId);
        Assert.Equal(places[5].Id, places[9].ParentId);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Keep_on_compression_retains_inactive_parents_without_changing_status(bool keep, bool activeRoot)
    {
        var root = Place(1, "root", activeRoot, 1);
        SetParentId(root, null);
        var inactive = Place(2, "inactive", false, 2);
        var child = Place(3, "child", true, 3);
        var system = Place(4, null, true, 4);
        var repository = new Repository([root, inactive, child, system]);
        var unit = new UnitOfWork();
        var activity = JsonSerializer.SerializeToElement(new { type = "marketing",
            when_inactive = new { keep_on_compression = keep } });
        var service = new StructureCompressionService(repository, new LockRepository(),
            new StructureQueries(activity, width: 1), new RankQueries(),
            new VolumeQueries(new Dictionary<string, uint>()), new PositionAlgorithmConfigurationParser(), unit, new Queries());
        var error = await service.CompressAsync("marketing", 1, default);
        Assert.Equal(activeRoot || keep, error is null);
        Assert.Equal(activeRoot, root.IsActive);
        Assert.False(inactive.IsActive);
        Assert.Equal(2L, inactive.ActivatedAt);
        Assert.True(child.IsActive);
        if (!activeRoot && !keep)
        {
            Assert.Equal(0, unit.SaveCount);
            Assert.Empty(repository.Removed);
            return;
        }
        Assert.Equal(1, unit.SaveCount);
        Assert.Equal(keep ? inactive.Id : root.Id, child.ParentId);
        Assert.Equal(keep ? 3 : 2, root.MatrixFilling);
        Assert.Contains(system, repository.Removed);
        Assert.Equal(!keep, repository.Removed.Contains(inactive));
    }

    [Fact]
    public async Task Retained_inactive_terminal_clone_cannot_become_parent_and_failure_does_not_mutate_places()
    {
        var root = Place(1, "root", true, 1);
        SetParentId(root, null);
        var terminal = Place(2, "terminal", false, 2);
        terminal.GetType().GetProperty(nameof(terminal.Kind))!.SetValue(terminal, PlaceKinds.TerminalClone);
        var child = Place(3, "child", true, 3);
        var oldMp = root.Mp;
        var repository = new Repository([root, terminal, child]);
        var unit = new UnitOfWork();
        var activity = JsonSerializer.Deserialize<JsonElement>("{\"type\":\"marketing\",\"when_inactive\":{\"keep_on_compression\":true}}");
        var service = new StructureCompressionService(repository, new LockRepository(),
            new StructureQueries(activity, width: 1), new RankQueries(),
            new VolumeQueries(new Dictionary<string, uint>()), new PositionAlgorithmConfigurationParser(), unit, new Queries());
        var error = await service.CompressAsync("marketing", 1, default);
        Assert.NotNull(error);
        Assert.Contains("found no position", error);
        Assert.Equal(0, unit.SaveCount);
        Assert.Empty(repository.Removed);
        Assert.Equal(oldMp, root.Mp);
        Assert.Equal(root.Id, child.ParentId);
        Assert.False(terminal.IsActive);
    }

    private sealed class Queries : PlaceQueriesStub { }

    private static Place Place(
        int id,
        string? profile,
        bool active,
        long? activatedAt)
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
            activatedAt);
        typeof(ReferalProgram.Core.PlaceAggregate.Place).GetProperty(nameof(place.Id))!
            .SetValue(place, id);
        return place;
    }

    private static void SetParentId(Place place, int? parentId) =>
        typeof(ReferalProgram.Core.PlaceAggregate.Place)
            .GetProperty(nameof(ReferalProgram.Core.PlaceAggregate.Place.ParentId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(place, parentId);

    private sealed class StructureQueries(JsonElement? activity = null, byte width = 2) : IStructureQueries
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
                Width = width,
                Activity = activity,
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

    private sealed class VolumeQueries(IReadOnlyDictionary<string, uint> volumes)
        : IProfileVolumeQueries
    {
        public Task<ProfileVolumeResponse> GetAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<string, uint>> GetReferralVolumesAsync(
            string marketingAddr,
            byte structureNumber,
            IReadOnlyCollection<string> profileAddresses,
            CancellationToken cancellationToken) =>
            Task.FromResult(volumes);
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
