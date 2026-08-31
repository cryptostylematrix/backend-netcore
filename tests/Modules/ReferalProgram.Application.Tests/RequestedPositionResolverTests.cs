using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class RequestedPositionResolverTests
{
    [Fact]
    public async Task Required_root_rejects_position_outside_profile_subtree()
    {
        var resolver = new RequestedPositionResolver(
            new Queries(Parent("OTHER", filling: 0)));

        var result = await resolver.ResolveAsync(
            "marketing",
            structureNumber: 2,
            structureWidth: 3,
            positionGroup: 1,
            new RequestedPosition(2, "parent", 1, 1),
            requiredRootMp: "ROOT",
            lockMps: [],
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("position_is_outside_viewer_root", result.Reason);
    }

    [Fact]
    public async Task Missing_required_root_allows_system_position_outside_algorithm_root()
    {
        var resolver = new RequestedPositionResolver(
            new Queries(Parent("OTHER", filling: 1)));

        var result = await resolver.ResolveAsync(
            "marketing",
            structureNumber: 2,
            structureWidth: 3,
            positionGroup: 2,
            new RequestedPosition(2, "parent", 1, 2),
            requiredRootMp: null,
            lockMps: [],
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("OTHER00000002", result.Position!.Mp);
        Assert.Equal((byte)2, result.Position.PosGroup);
    }

    private static PlaceResponse Parent(string mp, uint filling) => new()
    {
        MarketingAddr = "marketing",
        StructNumber = 2,
        ProfileAddr = "parent",
        ProfileLogin = "parent",
        PlaceNumber = 1,
        Mp = mp,
        Filling = filling
    };

    private sealed class Queries(PlaceResponse parent) : PlaceQueriesStub
    {
        public override Task<PlaceResponse?> GetPlaceAsync(
            string marketingAddr,
            byte structureNumber,
            string? profileAddr,
            uint placeNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<PlaceResponse?>(parent);
    }
}
