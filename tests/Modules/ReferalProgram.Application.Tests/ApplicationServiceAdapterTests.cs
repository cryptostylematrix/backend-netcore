using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Application.Services.RootStrategies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ApplicationServiceAdapterTests
{
    [Fact]
    public async Task Next_position_queries_delegate_to_their_specialized_queries()
    {
        var structure = new StructureResponse { MarketingAddr = "marketing", StructureNumber = 3 };
        var counts = new Dictionary<byte, long> { [1] = 7 };
        var structureQueries = new StructureQueriesStub(structure);
        var placeQueries = new CapturingPlaceQueries { Counts = counts };
        var service = new NextPositionQueries(structureQueries, placeQueries);

        var actualStructure = await service.GetStructureAsync("marketing", 3, default);
        var actualCounts = await service.GetPlaceCountsByPosGroupAsync("marketing", 3, default);

        Assert.Same(structure, actualStructure);
        Assert.Same(counts, actualCounts);
        Assert.Equal(("marketing", (byte)3), structureQueries.Call);
        Assert.Equal(("marketing", (byte)3), placeQueries.CountCall);
    }

    [Fact]
    public async Task Owner_root_strategy_loads_the_structure_root()
    {
        var root = new PlaceResponse { Mp = "root" };
        var queries = new CapturingPlaceQueries { Root = root };
        var strategy = new OwnerRootPlaceStrategy(queries);

        var result = await strategy.ResolveAsync(
            new RootPlaceStrategyContext("marketing", 4, "ignored"), default);

        Assert.Equal("owner", strategy.Name);
        Assert.Same(root, result);
        Assert.Equal(("marketing", (byte)4), queries.RootCall);
    }

    [Fact]
    public async Task Profile_root_strategy_forwards_the_profile_identity()
    {
        var root = new PlaceResponse { ProfileAddr = "profile" };
        var resolver = new CapturingProfileRootResolver(root);
        var strategy = new ProfileRootPlaceStrategy(resolver);

        var result = await strategy.ResolveAsync(
            new RootPlaceStrategyContext("marketing", 5, "profile"), default);

        Assert.Equal("profile", strategy.Name);
        Assert.Same(root, result);
        Assert.Equal(("marketing", (byte)5, "profile"), resolver.Call);
    }

    private sealed class StructureQueriesStub(StructureResponse? result) : IStructureQueries
    {
        public (string Marketing, byte Structure)? Call { get; private set; }

        public Task<StructureResponse?> GetStructureAsync(
            string marketingAddr, byte structureNumber, CancellationToken cancellationToken)
        {
            Call = (marketingAddr, structureNumber);
            return Task.FromResult(result);
        }
    }

    private sealed class CapturingPlaceQueries : PlaceQueriesStub
    {
        public IReadOnlyDictionary<byte, long> Counts { get; init; } = new Dictionary<byte, long>();
        public PlaceResponse? Root { get; init; }
        public (string Marketing, byte Structure)? CountCall { get; private set; }
        public (string Marketing, byte Structure)? RootCall { get; private set; }

        public override Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(
            string marketingAddr, byte structureNumber, CancellationToken cancellationToken)
        {
            CountCall = (marketingAddr, structureNumber);
            return Task.FromResult(Counts);
        }

        public override Task<PlaceResponse?> GetRootPlaceAsync(
            string marketingAddr, byte structureNumber, CancellationToken cancellationToken)
        {
            RootCall = (marketingAddr, structureNumber);
            return Task.FromResult(Root);
        }
    }

    private sealed class CapturingProfileRootResolver(PlaceResponse? result)
        : IProfileRootPlaceResolver
    {
        public (string Marketing, byte Structure, string? Profile)? Call { get; private set; }

        public Task<PlaceResponse?> ResolveAsync(
            string marketingAddr, byte structureNumber, string? profileAddr,
            CancellationToken cancellationToken)
        {
            Call = (marketingAddr, structureNumber, profileAddr);
            return Task.FromResult(result);
        }
    }
}
