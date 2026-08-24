using Common.Dto;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class RelativePlaceResolverTests
{
    [Fact]
    public async Task Resolve_skips_system_and_inactive_places_when_counting_levels()
    {
        var places = new[]
        {
            Place(1, null, "root", active: true, profileAddr: "profile-root"),
            Place(2, 1, "inactive", active: false, profileAddr: "profile-inactive"),
            Place(3, 2, "profile", active: true, profileAddr: "profile-current"),
            Place(4, 3, "system", active: true, profileAddr: null)
        };
        var resolver = new RelativePlaceResolver(new PlaceQueries(places));

        var result = await resolver.ResolveAsync(
            "marketing", 4, null, 4, level: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4, result.SourcePlace.Id);
        Assert.Equal("profile-root", result.RelativePlace.ProfileAddr);
    }

    [Fact]
    public async Task Resolve_returns_null_when_source_place_does_not_exist()
    {
        var resolver = new RelativePlaceResolver(new PlaceQueries([]));

        var result = await resolver.ResolveAsync(
            "marketing", 4, "missing", 1, level: 0, CancellationToken.None);

        Assert.Null(result);
    }

    private static PlaceResponse Place(
        int id,
        int? parentId,
        string mp,
        bool active,
        string? profileAddr) => new()
    {
        Id = id,
        ParentId = parentId,
        MarketingAddr = "marketing",
        StructNumber = 4,
        ProfileAddr = profileAddr,
        ProfileLogin = profileAddr is null ? null : $"login-{id}",
        PlaceNumber = checked((uint)id),
        Mp = mp,
        IsActive = active
    };

    private sealed class PlaceQueries(IEnumerable<PlaceResponse> places) : IPlaceQueries
    {
        private readonly IReadOnlyList<PlaceResponse> _places = places.ToArray();

        public Task<PlaceResponse?> GetPlaceAsync(
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

        public Task<PlaceResponse?> GetPlaceAsync(
            int id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_places.SingleOrDefault(place => place.Id == id));

        public Task<PlaceResponse?> GetFirstPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetLastPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Paginated<PlaceWithMatrixResponse>> GetPlacesAsync(string marketingAddr, byte structureNumber, string profileAddr, long matrixSize, bool isMatrixStructure, bool onlyNotClosed, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<long> GetPlacesCountAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasProfilePlacesInStructuresAsync(string marketingAddr, string profileAddr, IReadOnlyCollection<byte> structureNumbers, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, PlaceTreeCounts>> GetTreeCountsByMpAsync(string marketingAddr, byte structureNumber, IReadOnlyCollection<string> mpPrefixes, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, byte depthSpread, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, bool profiledPlacesPrioritized, byte depthSpread, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte width, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Paginated<PlaceResponse>> SearchPlacesAsync(string marketingAddr, byte structureNumber, string rootMp, string query, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetPlaceByTaskKeyAsync(string marketingAddr, int taskKey, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(string marketingAddr, byte structureNumber, string? fromProfileAddr, uint fromPlaceNumber, string? toProfileAddr, uint toPlaceNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte depthLevels, uint fromPos, uint toPos, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetRootPlaceAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Paginated<PlaceResponse>> GetChildrenAsync(string marketingAddr, byte structureNumber, string parentProfileAddr, uint parentPlaceNumber, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
