using Common.Dto;
using System.Text.Json;
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
        var resolver = new RelativePlaceResolver(new PlaceQueries(places), new StructureQueries());

        var result = await resolver.ResolveAsync(
            "marketing", 4, null, 4, level: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4, result.SourcePlace.Id);
        Assert.Equal("profile-root", result.RelativePlace.ProfileAddr);
    }

    [Fact]
    public async Task Resolve_returns_null_when_source_place_does_not_exist()
    {
        var resolver = new RelativePlaceResolver(new PlaceQueries([]), new StructureQueries());

        var result = await resolver.ResolveAsync(
            "marketing", 4, "missing", 1, level: 0, CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task Recipient_permissions_are_independent_and_count_only_eligible_profiled_levels(byte number)
    {
        foreach (var bonus in new[] { false, true })
        foreach (var clone in new[] { false, true })
        foreach (var purpose in new[] { RecipientPurpose.Bonus, RecipientPurpose.Clone })
        foreach (ushort level in new ushort[] { 0, 1, 2, 20 })
        {
            var places = new[]
            {
                Place(1, null, "root", true, "root", number),
                Place(2, 1, "inactive", false, "inactive", number),
                Place(3, 2, "system", true, null, number),
                Place(4, 3, "source", true, "source", number)
            };
            var json = JsonSerializer.SerializeToElement(new { type = number == 0 ? "invite" : "marketing",
                when_inactive = new { allow_as_bonus_recipient = bonus, allow_as_clone_recipient = clone } });
            var resolver = new RelativePlaceResolver(new PlaceQueries(places), new StructureQueries(json));
            var result = await resolver.ResolveAsync("marketing", number, "source", 4, level, default, purpose);
            var allows = purpose == RecipientPurpose.Bonus ? bonus : clone;
            Assert.Equal(level == 0 ? 4 : level == 1 && allows ? 2 : 1, result?.RelativePlace.Id);
            Assert.Equal(4, result?.SourcePlace.Id);
            Assert.False(places[1].IsActive);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("{\"set_active_on_activation\":false}")]
    [InlineData("{\"set_active_on_activation\":\"ignored-for-recipients\"}")]
    [InlineData("{\"type\":\"marketing\"}")]
    public async Task Defaults_preserve_inactive_root_and_system_exclusion(string? json)
    {
        var root = Place(1, null, "root", false, "root");
        var resolver = new RelativePlaceResolver(new PlaceQueries([root]),
            new StructureQueries(json is null ? null : JsonSerializer.Deserialize<JsonElement>(json)));
        foreach (var purpose in new[] { RecipientPurpose.Bonus, RecipientPurpose.Clone })
            Assert.Null(await resolver.ResolveAsync("marketing", 4, "root", 1, 0, default, purpose));
    }

    [Fact]
    public async Task Default_recipient_results_match_legacy_level_and_root_fallback_rules()
    {
        // Exhaust all active/inactive/system combinations in a five-place chain.
        foreach (var json in new string?[] { null, "{}", "{\"set_active_on_activation\":true}", "{\"type\":\"marketing\"}" })
        for (var pattern = 0; pattern < 243; pattern++)
        {
            var value = pattern;
            var places = new List<PlaceResponse>();
            for (var id = 1; id <= 5; id++)
            {
                var state = value % 3;
                value /= 3;
                places.Add(Place(id, id == 1 ? null : id - 1, id.ToString(), state != 0,
                    state == 2 ? null : "p" + id));
            }
            var resolver = new RelativePlaceResolver(new PlaceQueries(places),
                new StructureQueries(json is null ? null : JsonSerializer.Deserialize<JsonElement>(json)));
            var eligible = places.Where(p => p.IsActive && p.ProfileAddr is not null).Reverse().ToArray();
            var root = places[0];
            foreach (var purpose in new[] { RecipientPurpose.Bonus, RecipientPurpose.Clone })
            for (ushort level = 0; level <= 6; level++)
            {
                int? expected = level < eligible.Length ? eligible[level].Id
                    : root.IsActive && root.ProfileAddr is not null ? root.Id : null;
                var result = await resolver.ResolveAsync("marketing", 4, places[^1].ProfileAddr, 5, level, default, purpose);
                Assert.Equal(expected, result?.RelativePlace.Id);
            }
        }
    }

    private sealed class StructureQueries(JsonElement? activity = null) : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber,
            CancellationToken cancellationToken) => Task.FromResult<StructureResponse?>(new() { Activity = activity });
    }

    private static PlaceResponse Place(
        int id,
        int? parentId,
        string mp,
        bool active,
        string? profileAddr, byte structureNumber = 4) => new()
    {
        Id = id,
        ParentId = parentId,
        MarketingAddr = "marketing",
        StructNumber = structureNumber,
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
        public Task<IReadOnlySet<string>> GetActiveInviteProfilesAsync(string marketingAddr, IReadOnlyCollection<string> profileAddrs, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasProfilePlacesInStructuresAsync(string marketingAddr, string profileAddr, IReadOnlyCollection<byte> structureNumbers, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasProfilePlacesOutsideInviteStructureAsync(string marketingAddr, string profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, PlaceTreeCounts>> GetTreeCountsByMpAsync(string marketingAddr, byte structureNumber, IReadOnlyCollection<string> mpPrefixes, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, byte depthSpread, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken, PlacementActivityRules? activity = null) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, bool profiledPlacesPrioritized, byte depthSpread, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken, PlacementActivityRules? activity = null) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte width, int page, int pageSize, CancellationToken cancellationToken, PlacementActivityRules? activity = null) => throw new NotSupportedException();
        public Task<Paginated<PlaceResponse>> SearchPlacesAsync(string marketingAddr, byte structureNumber, string rootMp, string query, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(string marketingAddr, byte structureNumber, string? fromProfileAddr, uint fromPlaceNumber, string? toProfileAddr, uint toPlaceNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte depthLevels, uint fromPos, uint toPos, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetRootPlaceAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Paginated<PlaceResponse>> GetChildrenAsync(string marketingAddr, byte structureNumber, string parentProfileAddr, uint parentPlaceNumber, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
