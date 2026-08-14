using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Tests;

public sealed class SourcePlaceResolverTests
{
    [Fact]
    public async Task Resolves_ancestor_counts_its_depth_slice_and_maps_response()
    {
        var created = NewPlace("child", deep: 5, profileAddr: "buyer");
        var firstParent = NewPlace("parent-1", deep: 4, profileAddr: "parent-1", id: 11);
        var secondParent = NewPlace("parent-2", deep: 3, profileAddr: "parent-2", id: 12);
        var ancestor = NewPlace("source", deep: 2, profileAddr: "source-profile", id: 13);
        var repository = new Repository(
            [firstParent, secondParent, ancestor],
            count: 6);
        var resolver = new SourcePlaceResolver(repository);

        var result = await resolver.ResolveAsync(created, structureHeight: 3, default);

        Assert.NotNull(result);
        Assert.Equal((uint)7, result.Code);
        Assert.Equal("source", result.SourcePlace.Mp);
        Assert.Equal("source-profile", result.SourcePlace.ProfileAddr);
        Assert.Equal(3, repository.ParentLookupCount);
        Assert.Equal(("marketing", (byte)4, "source", (uint)5), repository.CountCall);
    }

    [Fact]
    public async Task Uses_highest_available_parent_when_configured_height_is_not_reachable()
    {
        var parent = NewPlace("top", deep: 1, profileAddr: "top-profile", id: 11);
        var repository = new Repository([parent, null], count: 0);
        var resolver = new SourcePlaceResolver(repository);

        var result = await resolver.ResolveAsync(
            NewPlace("child", deep: 2, profileAddr: "profile"), 2, default);

        Assert.NotNull(result);
        Assert.Equal("top", result.SourcePlace.Mp);
        Assert.Equal((uint)0, result.Code);
        Assert.Equal(2, repository.ParentLookupCount);
        Assert.Null(repository.CountCall);
    }

    [Fact]
    public async Task Counts_created_place_when_it_is_its_own_source()
    {
        var created = NewPlace("created", deep: 2, profileAddr: "profile");
        var repository = new Repository([], count: 0);
        var resolver = new SourcePlaceResolver(repository);

        var result = await resolver.ResolveAsync(created, structureHeight: 0, default);

        Assert.NotNull(result);
        Assert.Equal("created", result.SourcePlace.Mp);
        Assert.Equal((uint)1, result.Code);
        Assert.Equal(0, repository.ParentLookupCount);
        Assert.Equal(("marketing", (byte)4, "created", (uint)2), repository.CountCall);
    }

    [Fact]
    public async Task Throws_when_count_does_not_fit_command_response_code()
    {
        var place = NewPlace("source", deep: 1, profileAddr: "profile");
        var resolver = new SourcePlaceResolver(
            new Repository(
                [NewPlace("parent", deep: 0, profileAddr: "parent", id: 11)],
                uint.MaxValue));

        await Assert.ThrowsAsync<OverflowException>(() =>
            resolver.ResolveAsync(place, 1, default));
    }

    private static Place NewPlace(
        string mp,
        uint deep,
        string profileAddr,
        int id = 0)
    {
        var place = Place.Create(
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

        if (id != 0)
        {
            typeof(Place)
                .GetProperty(nameof(Place.Id))!
                .SetValue(place, id);
        }

        return place;
    }

    private sealed class Repository(
        IEnumerable<Place?> parents,
        long count) : PlaceRepositoryStub
    {
        private readonly Queue<Place?> _parents = new(parents);

        public int ParentLookupCount { get; private set; }
        public (string Marketing, byte Structure, string Mp, uint Depth)? CountCall { get; private set; }

        public override Task<Place?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            ParentLookupCount++;
            return Task.FromResult(_parents.Count > 0 ? _parents.Dequeue() : null);
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
