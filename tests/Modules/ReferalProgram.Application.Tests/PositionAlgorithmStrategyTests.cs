using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services.PositionStrategies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class PositionAlgorithmStrategyTests
{
    [Fact]
    public async Task Chess_prioritizes_profiled_candidates_when_configured()
    {
        var candidates = new CandidateQueries
        {
            DepthWindow =
            [
                Place("ROOT00000001", null, 1, filling: 0),
                Place("ROOT00000002", "profile", 2, filling: 0)
            ]
        };
        var strategy = new ChessPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(profiledFirst: true), CancellationToken.None);

        Assert.Equal("profile", result?.ProfileAddr);
        Assert.Equal("ROOT0000000200000001", result?.Mp);
    }

    [Fact]
    public async Task Radar_passes_depth_and_priority_and_builds_next_position()
    {
        var candidates = new CandidateQueries
        {
            DepthWindow =
            [
                Place("ROOT00000001", "profile", 7, filling: 2)
            ]
        };
        var strategy = new RadarPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(profiledFirst: false, depthSpread: 3), CancellationToken.None);

        Assert.Equal((byte)3, candidates.LastDepthSpread);
        Assert.Equal((uint)3, result?.Pos);
        Assert.Equal("ROOT0000000100000003", result?.Mp);
    }

    [Fact]
    public async Task Classic_skips_viewer_locked_branch()
    {
        var candidates = new CandidateQueries
        {
            OpenPlaces =
            [
                Place("ROOT00000001", "first", 1, filling: 0),
                Place("ROOT00000002", "second", 2, filling: 0)
            ]
        };
        var strategy = new ClassicPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(lockMps: ["ROOT0000000100000001"]),
            CancellationToken.None);

        Assert.Equal("second", result?.ProfileAddr);
        Assert.Equal("ROOT0000000200000001", result?.Mp);
    }

    [Fact]
    public async Task Trimmed_classic_preserves_classic_position_order()
    {
        var candidates = new CandidateQueries
        {
            OpenPlaces =
            [
                Place("ROOT00000001", "first", 1, filling: 1),
                Place("ROOT00000002", "second", 2, filling: 0)
            ]
        };
        var strategy = new TrimmedClassicPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(cutFactor: 2),
            CancellationToken.None);

        Assert.Equal("first", result?.ProfileAddr);
        Assert.Equal((uint)2, result?.Pos);
        Assert.Equal("ROOT0000000100000002", result?.Mp);
    }

    [Fact]
    public async Task Chess_skips_root_profile_locked_branch()
    {
        var candidates = new CandidateQueries
        {
            DepthWindow =
            [
                Place("ROOT00000001", "first", 1, filling: 0),
                Place("ROOT00000002", "second", 2, filling: 0)
            ]
        };
        var strategy = new ChessPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(lockMps: ["ROOT0000000100000001"]),
            CancellationToken.None);

        Assert.Equal("second", result?.ProfileAddr);
    }

    [Fact]
    public async Task Radar_skips_root_profile_locked_branch()
    {
        var candidates = new CandidateQueries
        {
            DepthWindow =
            [
                Place("ROOT00000001", "first", 1, filling: 0),
                Place("ROOT00000002", "second", 2, filling: 0)
            ]
        };
        var strategy = new RadarPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(lockMps: ["ROOT0000000100000001"]),
            CancellationToken.None);

        Assert.Equal("second", result?.ProfileAddr);
    }

    [Fact]
    public async Task Chess_rejects_zero_depth_spread()
    {
        var strategy = new ChessPositionAlgorithmStrategy(new CandidateQueries());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.FindNextAsync(Context(depthSpread: 0), CancellationToken.None));

        Assert.Contains("depth", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_frontier_passes_limit_and_builds_next_position()
    {
        var candidates = new CandidateQueries
        {
            ProfileFrontierCandidate = Place(
                "ROOT00000001",
                "profile",
                7,
                filling: 1)
        };
        var strategy = new ProfileFrontierPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(profiledFrontierLimit: 35),
            CancellationToken.None);

        Assert.Equal((uint)35, candidates.LastProfiledFrontierLimit);
        Assert.Equal("profile", result?.ProfileAddr);
        Assert.Equal((uint)2, result?.Pos);
        Assert.Equal("ROOT0000000100000002", result?.Mp);
    }

    [Fact]
    public async Task Profile_frontier_requires_a_positive_limit()
    {
        var strategy = new ProfileFrontierPositionAlgorithmStrategy(
            new CandidateQueries());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            strategy.FindNextAsync(Context(), CancellationToken.None));

        Assert.Contains("limit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task System_gap_builds_the_selected_top_left_position()
    {
        var candidates = new CandidateQueries
        {
            SystemGapCandidate = Place(
                "ROOT00000001",
                "profile",
                3,
                filling: 0)
        };
        var strategy = new SystemGapPositionAlgorithmStrategy(candidates);

        var result = await strategy.FindNextAsync(
            Context(),
            CancellationToken.None);

        Assert.Equal("profile", result?.ProfileAddr);
        Assert.Equal((uint)1, result?.Pos);
        Assert.Equal("ROOT0000000100000001", result?.Mp);
    }

    private static PositionAlgorithmStrategyContext Context(
        bool profiledFirst = true,
        byte depthSpread = 1,
        string[]? lockMps = null,
        uint? cutFactor = null,
        uint? profiledFrontierLimit = null) => new(
            "marketing",
            4,
            3,
            Place("ROOT", "root", 1, filling: 0),
            2,
            profiledFirst,
            depthSpread,
            lockMps ?? [],
            cutFactor,
            profiledFrontierLimit);

    private static PlaceResponse Place(
        string mp,
        string? profileAddr,
        uint number,
        uint filling) => new()
    {
        MarketingAddr = "marketing",
        StructNumber = 4,
        Mp = mp,
        ProfileAddr = profileAddr,
        PlaceNumber = number,
        Filling = filling,
        IsActive = true
    };

    private sealed class CandidateQueries : IPositionCandidateQueries
    {
        public IReadOnlyList<PlaceResponse> DepthWindow { get; init; } = [];
        public IReadOnlyList<PlaceResponse> OpenPlaces { get; init; } = [];
        public PlaceResponse? ProfileFrontierCandidate { get; init; }
        public PlaceResponse? SystemGapCandidate { get; init; }
        public byte LastDepthSpread { get; private set; }
        public uint LastProfiledFrontierLimit { get; private set; }

        public Task<PlaceResponse?> GetProfileFrontierCandidateAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            uint profiledFrontierLimit, IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken)
        {
            LastProfiledFrontierLimit = profiledFrontierLimit;
            return Task.FromResult(ProfileFrontierCandidate);
        }

        public Task<PlaceResponse?> GetSystemGapCandidateAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken) =>
            Task.FromResult(SystemGapCandidate);

        public Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            byte depthSpread, IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken)
        {
            LastDepthSpread = depthSpread;
            return Task.FromResult(DepthWindow);
        }

        public Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(
            string marketingAddr, byte structureNumber, string rootMp, byte width,
            bool profiledPlacesPrioritized, byte depthSpread,
            IReadOnlyCollection<string> lockMps,
            CancellationToken cancellationToken)
        {
            LastDepthSpread = depthSpread;
            return Task.FromResult(DepthWindow.FirstOrDefault(candidate =>
                !lockMps.Any(lockMp =>
                    (candidate.Mp + checked(candidate.Filling + 1).ToString("X8"))
                        .StartsWith(lockMp, StringComparison.Ordinal))));
        }

        public Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
            string marketingAddr, byte structureNumber, string mpPrefix, byte width,
            int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(page == 1 ? OpenPlaces : (IReadOnlyList<PlaceResponse>)[]);
    }

}
