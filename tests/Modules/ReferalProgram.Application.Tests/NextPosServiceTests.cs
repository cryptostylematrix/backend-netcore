using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class NextPosServiceTests
{
    [Fact]
    public async Task Orchestrator_passes_selected_group_and_resolved_root_to_algorithm()
    {
        var root = new PlaceResponse
        {
            MarketingAddr = "marketing",
            StructNumber = 4,
            ProfileAddr = "root",
            PlaceNumber = 1,
            Mp = "ROOT"
        };
        var algorithm = new CapturingAlgorithm();
        var locks = new LockQueries("LOCK");
        var service = new NextPosService(
            new Queries(Structure(), new Dictionary<byte, long> { [0] = 5, [1] = 0 }),
            new PositionAlgorithmConfigurationParser(),
            new PositionGroupSelector(),
            new RootResolver(root),
            new AlgorithmResolver(algorithm),
            locks);

        var result = await service.GetNextPosAsync(
            "marketing", 4, "viewer", operation: null, CancellationToken.None);

        Assert.Same(algorithm.Result, result);
        Assert.NotNull(algorithm.Context);
        Assert.Same(root, algorithm.Context.Root);
        Assert.Equal((byte)1, algorithm.Context.PosGroup);
        Assert.Equal((byte)3, algorithm.Context.Width);
        Assert.Equal((byte)2, algorithm.Context.DepthSpread);
        Assert.False(algorithm.Context.ProfiledPlacesPrioritized);
        Assert.Equal((uint)35, algorithm.Context.ProfiledFrontierLimit);
        Assert.Equal(["LOCK"], algorithm.Context.RootProfileLockMps);
        Assert.Equal("root", locks.ProfileAddr);
    }

    [Fact]
    public async Task Orchestrator_returns_null_when_structure_does_not_exist()
    {
        var service = new NextPosService(
            new Queries(null, new Dictionary<byte, long>()),
            new PositionAlgorithmConfigurationParser(),
            new PositionGroupSelector(),
            new RootResolver(null),
            new AlgorithmResolver(new CapturingAlgorithm()),
            new LockQueries());

        var result = await service.GetNextPosAsync(
            "marketing", 4, "viewer", operation: null, CancellationToken.None);

        Assert.Null(result);
    }

    private static StructureResponse Structure()
    {
        using var document = JsonDocument.Parse("""
            {
              "v": 1,
              "root": "profile",
              "relation": "relative",
              "groups": [
                { "id": 0, "algo": "capturing", "weight": 1 },
                {
                  "id": 1,
                  "algo": "capturing",
                  "weight": 1,
                  "profiled_places_prioritized": false,
                  "depth_spread": 2,
                  "profiled_frontier_limit": 35
                }
              ]
            }
            """);

        return new StructureResponse
        {
            MarketingAddr = "marketing",
            StructureNumber = 4,
            Width = 3,
            PosAlgo = document.RootElement.Clone()
        };
    }

    private sealed class Queries(
        StructureResponse? structure,
        IReadOnlyDictionary<byte, long> counts) : INextPositionQueries
    {
        public Task<StructureResponse?> GetStructureAsync(
            string marketingAddr, byte structureNumber,
            CancellationToken cancellationToken) => Task.FromResult(structure);

        public Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(
            string marketingAddr, byte structureNumber,
            CancellationToken cancellationToken) => Task.FromResult(counts);
    }

    private sealed class RootResolver(PlaceResponse? root) : IPositionRootResolver
    {
        public Task<PlaceResponse?> ResolveAsync(
            string strategyName, string marketingAddr, byte structureNumber,
            string? profileAddr, CancellationToken cancellationToken) =>
            Task.FromResult(root);
    }

    private sealed class AlgorithmResolver(IPositionAlgorithmStrategy algorithm)
        : IPositionAlgorithmResolver
    {
        public IPositionAlgorithmStrategy Resolve(string name)
        {
            Assert.Equal("capturing", name);
            return algorithm;
        }
    }

    private sealed class LockQueries(params string[] lockMps) : IPositionLockQueries
    {
        public string? ProfileAddr { get; private set; }

        public Task<string[]> GetAllLockMpsAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            CancellationToken cancellationToken)
        {
            ProfileAddr = profileAddr;
            return Task.FromResult(lockMps);
        }
    }

    private sealed class CapturingAlgorithm : IPositionAlgorithmStrategy
    {
        public string Name => "capturing";
        public PositionAlgorithmStrategyContext? Context { get; private set; }
        public NextPosResponse Result { get; } = new()
        {
            ProfileAddr = "parent",
            PlaceNumber = 5,
            Pos = 2,
            Mp = "ROOT00000002"
        };

        public Task<NextPosResponse?> FindNextAsync(
            PositionAlgorithmStrategyContext context,
            CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult<NextPosResponse?>(Result);
        }
    }
}
