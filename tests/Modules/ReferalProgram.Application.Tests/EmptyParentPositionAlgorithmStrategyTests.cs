using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services.PositionStrategies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class EmptyParentPositionAlgorithmStrategyTests
{
    [Fact]
    public async Task Selects_topmost_leftmost_unlocked_parent_with_no_children()
    {
        var queries = new CandidateQueries(
        [
            Place(1, "0000000000000001", deep: 2, filling: 1),
            Place(2, "0000000000000002", deep: 2, filling: 0),
            Place(3, "0000000000000003", deep: 2, filling: 0),
            Place(4, "000000000000000100000001", deep: 3, filling: 0)
        ]);
        var strategy = new EmptyParentPositionAlgorithmStrategy(queries);
        var context = new PositionAlgorithmStrategyContext(
            "marketing",
            1,
            2,
            Place(100, "00000000", deep: 1, filling: 2),
            0,
            true,
            1,
            ["000000000000000200000001"],
            null,
            null);

        var result = await strategy.FindNextAsync(context, default);

        Assert.NotNull(result);
        Assert.Equal((uint)3, result.PlaceNumber);
        Assert.Equal("000000000000000300000001", result.Mp);
        Assert.Equal((uint)1, result.Pos);
    }

    private static PlaceResponse Place(int id, string mp, uint deep, uint filling) => new()
    {
        Id = id,
        MarketingAddr = "marketing",
        StructNumber = 1,
        ProfileAddr = $"profile-{id}",
        ProfileLogin = $"profile-{id}",
        PlaceNumber = checked((uint)id),
        Mp = mp,
        Deep = deep,
        Filling = filling,
        IsActive = true
    };

    private sealed class CandidateQueries(IReadOnlyList<PlaceResponse> candidates)
        : IPositionCandidateQueries
    {
        public Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
            string marketingAddr,
            byte structureNumber,
            string mpPrefix,
            byte width,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlaceResponse>>(page == 1 ? candidates : []);

        public Task<PlaceResponse?> GetProfileFrontierCandidateAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, uint profiledFrontierLimit, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetSystemGapCandidateAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, byte depthSpread, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, bool profiledPlacesPrioritized, byte depthSpread, IReadOnlyCollection<string> lockMps, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
