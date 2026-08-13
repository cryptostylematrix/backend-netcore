using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class SourcePlaceResolverTests
{
    [Fact]
    public async Task Resolves_ancestor_counts_its_depth_slice_and_maps_response()
    {
        var created = NewPlace("child", deep: 5, profileAddr: "buyer");
        var ancestor = NewPlace("source", deep: 2, profileAddr: "source-profile");
        var repository = new Repository(ancestor, count: 7);
        var resolver = new SourcePlaceResolver(repository);

        var result = await resolver.ResolveAsync(created, structureHeight: 3, default);

        Assert.NotNull(result);
        Assert.Equal((uint)7, result.Code);
        Assert.Equal("source", result.SourcePlace.Mp);
        Assert.Equal("source-profile", result.SourcePlace.ProfileAddr);
        Assert.Equal((created, (byte)3), repository.AncestorCall);
        Assert.Equal(("marketing", (byte)4, "source", (uint)5), repository.CountCall);
    }

    [Fact]
    public async Task Returns_null_when_required_ancestor_does_not_exist()
    {
        var repository = new Repository(null, count: 0);
        var resolver = new SourcePlaceResolver(repository);

        var result = await resolver.ResolveAsync(
            NewPlace("child", deep: 1, profileAddr: "profile"), 2, default);

        Assert.Null(result);
        Assert.Null(repository.CountCall);
    }

    [Fact]
    public async Task Throws_when_count_does_not_fit_command_response_code()
    {
        var place = NewPlace("source", deep: 1, profileAddr: "profile");
        var resolver = new SourcePlaceResolver(
            new Repository(place, (long)uint.MaxValue + 1));

        await Assert.ThrowsAsync<OverflowException>(() =>
            resolver.ResolveAsync(place, 0, default));
    }

    private static Place NewPlace(string mp, uint deep, string profileAddr) => Place.Create(
        parentId: 1,
        marketingAddr: "marketing",
        structureNumber: 4,
        profileAddr,
        profileLogin: $"login-{profileAddr}",
        index: $"index-{mp}",
        placeNumber: 2,
        parentProfileAddr: "parent",
        parentProfileLogin: "parent-login",
        parentPlaceNumber: 1,
        mp,
        posGroup: 2,
        kind: 1,
        pos: 1,
        filling: 0,
        deep,
        isActive: true,
        createdAt: 100,
        activatedAt: 100,
        personalVolume: 3,
        groupVolume: 4,
        taskKey: 5,
        taskQueryId: 6,
        taskSourceAddr: "wallet");

    private sealed class Repository(Place? ancestor, long count) : PlaceRepositoryStub
    {
        public (Place Place, byte Levels)? AncestorCall { get; private set; }
        public (string Marketing, byte Structure, string Mp, uint Depth)? CountCall { get; private set; }

        public override Task<Place?> GetAncestorAsync(
            Place place, byte levels, CancellationToken cancellationToken)
        {
            AncestorCall = (place, levels);
            return Task.FromResult(ancestor);
        }

        public override Task<long> CountAtDepthAsync(
            string marketingAddr, byte structureNumber, string mpPrefix,
            uint depth, CancellationToken cancellationToken)
        {
            CountCall = (marketingAddr, structureNumber, mpPrefix, depth);
            return Task.FromResult(count);
        }
    }
}
