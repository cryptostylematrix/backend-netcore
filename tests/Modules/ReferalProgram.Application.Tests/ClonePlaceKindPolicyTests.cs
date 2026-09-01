using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ClonePlaceKindPolicyTests
{
    [Theory]
    [InlineData(0, PlaceKinds.Clone)]
    [InlineData(1, PlaceKinds.TerminalClone)]
    [InlineData(2, PlaceKinds.Clone)]
    [InlineData(3, PlaceKinds.TerminalClone)]
    public async Task Every_second_clone_is_terminal(
        long existingCloneChildren,
        byte expectedKind)
    {
        var repository = new Repository
        {
            CloneChildrenCount = existingCloneChildren
        };
        var policy = new ClonePlaceKindPolicy(repository);

        var result = await policy.ResolveAsync(
            Selection("trimmed_classic", cutFactor: 2),
            parentId: 42,
            CancellationToken.None);

        Assert.Equal(expectedKind, result);
        Assert.Equal(42, repository.CountedParentId);
    }

    [Fact]
    public async Task Non_trimmed_algorithm_always_creates_an_ordinary_clone()
    {
        var repository = new Repository { CloneChildrenCount = 99 };
        var policy = new ClonePlaceKindPolicy(repository);

        var result = await policy.ResolveAsync(
            Selection("classic", cutFactor: null),
            parentId: 42,
            CancellationToken.None);

        Assert.Equal(PlaceKinds.Clone, result);
        Assert.Null(repository.CountedParentId);
    }

    [Fact]
    public void Terminal_clone_rejects_a_child()
    {
        var terminalClone = Place.Create(
            parentId: 1,
            marketingAddr: "marketing",
            structureNumber: 1,
            profileAddr: "profile",
            profileLogin: "login",
            index: "login2",
            placeNumber: 2,
            parentProfileAddr: "parent-profile",
            parentProfileLogin: "parent-login",
            parentPlaceNumber: 1,
            mp: "0000000000000001",
            posGroup: 0,
            kind: PlaceKinds.TerminalClone,
            pos: 1,
            filling: 0,
            deep: 2,
            isActive: true,
            createdAt: 1,
            activatedAt: 1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            terminalClone.RegisterChild(expectedFilling: 0));

        Assert.Contains("terminal clone", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static PositionSelection Selection(string algorithm, uint? cutFactor) =>
        new(
            algorithm,
            new PositionAlgorithmStrategyContext(
                "marketing",
                1,
                2,
                new PlaceResponse
                {
                    Id = 1,
                    MarketingAddr = "marketing",
                    StructNumber = 1,
                    Mp = "00000000",
                    ProfileAddr = "root-profile",
                    PlaceNumber = 1
                },
                PosGroup: 0,
                ProfiledPlacesPrioritized: true,
                DepthSpread: 1,
                RootProfileLockMps: [],
                CutFactor: cutFactor));

    private sealed class Repository : PlaceRepositoryStub
    {
        public long CloneChildrenCount { get; init; }
        public int? CountedParentId { get; private set; }

        public override Task<long> CountCloneChildrenAsync(
            int parentId,
            CancellationToken cancellationToken)
        {
            CountedParentId = parentId;
            return Task.FromResult(CloneChildrenCount);
        }
    }
}
