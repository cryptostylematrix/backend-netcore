using ReferalProgram.Application.Services;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ProfileRootPlaceResolverTests
{
    [Fact]
    public async Task Returns_the_profiles_first_place_when_it_exists()
    {
        var root = Place(10, null, 4, "profile", active: true);
        var resolver = new ProfileRootPlaceResolver(new Queries([root]));

        var result = await resolver.ResolveAsync("marketing", 4, "profile", default);

        Assert.Same(root, result);
    }

    [Fact]
    public async Task Walks_to_the_first_active_profiled_inviter()
    {
        var requestedInvite = Place(1, 2, 0, "requested", active: true);
        var systemInviter = Place(2, 3, 0, null, active: true);
        var inactiveInviter = Place(3, 4, 0, "inactive", active: false);
        var activeInviter = Place(4, null, 0, "inviter", active: true);
        var inviterRoot = Place(20, null, 4, "inviter", active: true);
        var resolver = new ProfileRootPlaceResolver(new Queries(
            [requestedInvite, systemInviter, inactiveInviter, activeInviter, inviterRoot]));

        var result = await resolver.ResolveAsync("marketing", 4, "requested", default);

        Assert.Same(inviterRoot, result);
    }

    [Fact]
    public async Task Returns_null_for_a_cycle_in_the_inviter_chain()
    {
        var firstInvite = Place(1, 2, 0, "first", active: true);
        var secondInvite = Place(2, 1, 0, "second", active: false);
        var resolver = new ProfileRootPlaceResolver(new Queries([firstInvite, secondInvite]));

        var result = await resolver.ResolveAsync("marketing", 4, "first", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Null_profile_uses_the_system_first_place_only()
    {
        var systemRoot = Place(30, null, 4, null, active: true);
        var resolver = new ProfileRootPlaceResolver(new Queries([systemRoot]));

        var result = await resolver.ResolveAsync("marketing", 4, "  ", default);

        Assert.Same(systemRoot, result);
    }

    private static PlaceResponse Place(
        int id,
        int? parentId,
        byte structure,
        string? profile,
        bool active) => new()
    {
        Id = id,
        ParentId = parentId,
        MarketingAddr = "marketing",
        StructNumber = structure,
        ProfileAddr = profile,
        ProfileLogin = profile,
        PlaceNumber = 1,
        IsActive = active,
        Mp = id.ToString()
    };

    private sealed class Queries(IEnumerable<PlaceResponse> places) : PlaceQueriesStub
    {
        private readonly IReadOnlyList<PlaceResponse> _places = places.ToArray();

        public override Task<PlaceResponse?> GetPlaceAsync(
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

        public override Task<PlaceResponse?> GetPlaceAsync(
            int id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_places.SingleOrDefault(place => place.Id == id));
    }
}
