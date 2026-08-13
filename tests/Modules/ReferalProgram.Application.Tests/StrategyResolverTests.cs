using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class StrategyResolverTests
{
    [Fact]
    public void Algorithm_resolver_is_case_insensitive()
    {
        var expected = new StubAlgorithm("future-algorithm");
        var resolver = new PositionAlgorithmResolver([expected]);

        var actual = resolver.Resolve("FUTURE-ALGORITHM");

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Algorithm_resolver_rejects_duplicate_names()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PositionAlgorithmResolver([
                new StubAlgorithm("same"),
                new StubAlgorithm("SAME")
            ]));

        Assert.Contains("more than once", exception.Message);
    }

    [Fact]
    public async Task Root_resolver_dispatches_by_name()
    {
        var root = TestPlace("ROOT", "profile", 1);
        var strategy = new StubRootStrategy("future-root", root);
        var resolver = new PositionRootResolver([strategy]);

        var actual = await resolver.ResolveAsync(
            "FUTURE-ROOT", "marketing", 4, "viewer", CancellationToken.None);

        Assert.Same(root, actual);
        Assert.Equal("viewer", strategy.LastContext?.ProfileAddr);
    }

    private sealed class StubAlgorithm(string name) : IPositionAlgorithmStrategy
    {
        public string Name => name;

        public Task<NextPosResponse?> FindNextAsync(
            PositionAlgorithmStrategyContext context,
            CancellationToken cancellationToken) => Task.FromResult<NextPosResponse?>(null);
    }

    private sealed class StubRootStrategy(string name, PlaceResponse root) : IRootPlaceStrategy
    {
        public string Name => name;
        public RootPlaceStrategyContext? LastContext { get; private set; }

        public Task<PlaceResponse?> ResolveAsync(
            RootPlaceStrategyContext context,
            CancellationToken cancellationToken)
        {
            LastContext = context;
            return Task.FromResult<PlaceResponse?>(root);
        }
    }

    private static PlaceResponse TestPlace(string mp, string? profileAddr, uint placeNumber) =>
        new()
        {
            MarketingAddr = "marketing",
            StructNumber = 4,
            Mp = mp,
            ProfileAddr = profileAddr,
            PlaceNumber = placeNumber
        };
}
