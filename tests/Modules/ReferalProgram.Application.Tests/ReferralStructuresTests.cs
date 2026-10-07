using System.Text.Json;
using Common.Dto;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Invites;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ReferralStructuresTests
{
    [Fact]
    public async Task Page_loads_memberships_in_one_batch_and_preserves_partners_without_places()
    {
        var queries = new Queries();
        var result = await new GetReferralsQueryHandler(queries).Handle(new("program", "parent", 2, 20), default);
        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToArray();
        Assert.Equal(new byte[] { 1, 3 }, items[0].StructureNumbers);
        Assert.Empty(items[1].StructureNumbers);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(1, queries.BatchCalls);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(items[0]));
        Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("structure_numbers").ValueKind);
    }

    [Fact]
    public async Task Inactive_root_also_receives_its_structure_memberships()
    {
        var queries = new Queries();
        var result = await new GetInviteInfoQueryHandler(queries).Handle(new("program", "first"), default);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Null(result.Value.ActivatedAt);
        Assert.Equal(new byte[] { 1, 3 }, result.Value.StructureNumbers);
    }

    private sealed class Queries : PlaceQueriesStub, IPlaceQueries
    {
        public int BatchCalls { get; private set; }

        public Task<IReadOnlyDictionary<string, byte[]>> GetProfileStructureNumbersAsync(
            string marketingAddr, IReadOnlyCollection<string> profileAddrs, CancellationToken cancellationToken)
        {
            Assert.Equal("program", marketingAddr);
            Assert.Contains("first", profileAddrs);
            BatchCalls++;
            return Task.FromResult<IReadOnlyDictionary<string, byte[]>>(
                new Dictionary<string, byte[]> { ["first"] = [1, 3] });
        }

        public override Task<PlaceResponse?> GetPlaceAsync(string marketingAddr, byte structureNumber,
            string? profileAddr, uint placeNumber, CancellationToken cancellationToken) =>
            Task.FromResult<PlaceResponse?>(new() { ProfileAddr = "first", ProfileLogin = "first" });

        public override Task<Paginated<PlaceResponse>> GetChildrenAsync(string marketingAddr, byte structureNumber,
            string parentProfileAddr, uint parentPlaceNumber, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new Paginated<PlaceResponse>
            {
                Items = [new() { ProfileAddr = "first" }, new() { ProfileAddr = "second" }],
                Page = 2,
                TotalPages = 3
            });
    }
}
