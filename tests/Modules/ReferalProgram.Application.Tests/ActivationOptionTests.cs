using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Application.Policies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ActivationOptionTests
{
    [Theory]
    [InlineData(null, 3, 7)]
    [InlineData("place", 3, 7)]
    [InlineData("group_root", 1, 1)]
    [InlineData("invite", 0, 1)]
    public async Task Resolves_direct_target_and_uses_its_command(string? source, byte target, uint number)
    {
        var structures = new Structures(source);
        var places = new Places();
        // Only the target structure exposes activation, not necessarily the viewed structure.
        var handler = Handler(structures, places, target);
        var result = await handler.Handle(new("mini", 3, "profile", 7), default);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanActivate);
        Assert.Equal(target, result.Value.StructureNumber);
        Assert.Equal(number, result.Value.PlaceNumber);
        Assert.Equal("profile", result.Value.ProfileAddr);
        Assert.Equal(ProgramCommandTags.ActivatePlace, result.Value.CommandTag);
        Assert.All(places.Reads, read => Assert.Equal("mini:profile", read.Scope));
    }

    [Fact]
    public async Task Viewed_place_date_does_not_prevent_group_root_activation()
    {
        var places = new Places { ViewedActivated = true };
        var result = await Handler(new("group_root"), places, 1).Handle(new("mini", 3, "profile", 7), default);
        Assert.True(result.Value.CanActivate);
    }

    [Theory]
    [InlineData(true, false, true, "place_already_activated")]
    [InlineData(false, true, true, "place_not_found")]
    [InlineData(false, false, false, "activation_command_not_configured")]
    public async Task Cannot_fall_back_to_viewed_place_when_source_is_unavailable(
        bool activated, bool missing, bool command, string reason)
    {
        var places = new Places { TargetActivated = activated, MissingTarget = missing };
        var result = await Handler(new("group_root"), places, command ? (byte)1 : (byte)3)
            .Handle(new("mini", 3, "profile", 7), default);
        Assert.False(result.Value.CanActivate);
        Assert.Equal(reason, result.Value.Reason);
        Assert.Equal((byte)1, result.Value.StructureNumber);
        Assert.Null(result.Value.CommandTag);
    }

    [Fact]
    public async Task Group_root_uses_first_place_even_when_viewing_another_place_of_root_structure()
    {
        var result = await Handler(new("group_root"), new Places(), 1)
            .Handle(new("mini", 1, "profile", 7), default);
        Assert.True(result.Value.CanActivate);
        Assert.Equal(1u, result.Value.PlaceNumber);
    }

    [Fact]
    public async Task Source_is_not_followed_recursively()
    {
        var structures = new Structures("group_root") { RootSource = "invite" };
        var result = await Handler(structures, new Places(), 1).Handle(new("mini", 3, "profile", 7), default);
        Assert.True(result.Value.CanActivate);
        Assert.Equal((byte)1, result.Value.StructureNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Missing_group_is_denied(string? group)
    {
        var result = await Handler(new("group_root") { Group = group }, new Places(), 1)
            .Handle(new("mini", 3, "profile", 7), default);
        Assert.False(result.Value.CanActivate);
        Assert.Equal("activity_configuration_invalid", result.Value.Reason);
    }

    [Fact]
    public async Task Missing_settings_hide_activation()
    {
        var result = await Handler(new(null) { MissingSettings = true }, new Places(), 3)
            .Handle(new("mini", 3, "profile", 7), default);
        Assert.False(result.Value.CanActivate);
        Assert.Equal("activity_configuration_missing", result.Value.Reason);
    }

    private static GetActivationOptionQueryHandler Handler(Structures structures, Places places, byte target) =>
        new(structures, places, new ActivatePlacePolicy(structures, places, new Commands(target)));

    private sealed class Structures(string? source) : IStructureQueries
    {
        public string? Group { get; init; } = "Mini 10";
        public string? RootSource { get; init; }
        public bool MissingSettings { get; init; }
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber, CancellationToken ct)
        {
            var activitySource = structureNumber == 1 ? RootSource ?? source : source;
            return Task.FromResult<StructureResponse?>(new() {
                MarketingAddr = marketingAddr, StructureNumber = structureNumber, Group = Group,
                Activity = MissingSettings ? null : activitySource is null ? JsonSerializer.Deserialize<JsonElement>("{}")
                    : JsonSerializer.SerializeToElement(new { type = structureNumber == 0 ? "invite" : "marketing", activity_source = activitySource })
            });
        }
    }
    private sealed class Places : PlaceQueriesStub, IPlaceQueries
    {
        public bool TargetActivated { get; init; }
        public bool ViewedActivated { get; init; }
        public bool MissingTarget { get; init; }
        public List<(string Scope, byte Structure, uint Place)> Reads { get; } = [];
        public Task<byte?> GetGroupRootStructureAsync(string marketingAddr, byte structureNumber, CancellationToken ct) => Task.FromResult<byte?>(1);
        public override Task<PlaceResponse?> GetPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, uint placeNumber, CancellationToken ct)
        {
            Reads.Add(($"{marketingAddr}:{profileAddr}", structureNumber, placeNumber));
            return Task.FromResult<PlaceResponse?>(MissingTarget && placeNumber == 1 ? null : new() {
                MarketingAddr = marketingAddr, StructNumber = structureNumber, ProfileAddr = profileAddr, PlaceNumber = placeNumber,
                ActivatedAt = (placeNumber == 1 ? TargetActivated : ViewedActivated) ? 123 : null
            });
        }
    }
    private sealed class Commands(byte target) : IProgramCommandQueries
    {
        public Task<ProgramCommandConfiguration> GetConfigurationAsync(string marketingAddr, CancellationToken ct) =>
            Task.FromResult(new ProgramCommandConfiguration(new Dictionary<byte, IReadOnlySet<uint>> {
                [target] = new HashSet<uint> { ProgramCommandTags.ActivatePlace }
            }));
    }
}
