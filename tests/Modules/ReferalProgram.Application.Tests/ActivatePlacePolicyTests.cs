using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class ActivatePlacePolicyTests
{
    [Fact]
    public void Allows_profile_place_when_command_and_activity_are_configured()
    {
        var result = Policy().Evaluate(
            Structure("{}"),
            new HashSet<uint> { ProgramCommandTags.ActivatePlace },
            Place());

        Assert.True(result.CanActivate);
        Assert.True(result.SetActiveOnActivation);
        Assert.Equal(ProgramCommandTags.ActivatePlace, result.CommandTag);
    }

    [Fact]
    public void Respects_set_active_on_activation_false()
    {
        var result = Policy().Evaluate(
            Structure("{\"set_active_on_activation\":false}"),
            new HashSet<uint> { ProgramCommandTags.ActivatePlace },
            Place());

        Assert.True(result.CanActivate);
        Assert.False(result.SetActiveOnActivation);
    }

    [Theory]
    [InlineData(false, false, null, "activation_command_not_configured")]
    [InlineData(true, false, null, "activity_configuration_missing")]
    [InlineData(true, true, 1L, "place_already_activated")]
    public void Denies_invalid_activation_state(
        bool commandConfigured,
        bool activityConfigured,
        long? activatedAt,
        string reason)
    {
        var result = Policy().Evaluate(
            Structure(activityConfigured ? "{}" : null),
            commandConfigured
                ? new HashSet<uint> { ProgramCommandTags.ActivatePlace }
                : new HashSet<uint>(),
            Place(activatedAt: activatedAt));

        Assert.False(result.CanActivate);
        Assert.Equal(reason, result.Reason);
    }

    [Theory]
    [InlineData("structure")]
    [InlineData("group")]
    [InlineData("program")]
    [InlineData("unknown")]
    public void Retired_sync_setting_is_ignored(string oldSetting)
    {
        var result = Policy().Evaluate(
            Structure(JsonSerializer.Serialize(new { activation_sync = oldSetting })),
            new HashSet<uint> { ProgramCommandTags.ActivatePlace }, Place());
        Assert.True(result.CanActivate);
        Assert.True(result.SetActiveOnActivation);
    }

    [Fact]
    public void Denies_system_place()
    {
        var result = Policy().Evaluate(
            Structure("{}"),
            new HashSet<uint> { ProgramCommandTags.ActivatePlace },
            Place(profileAddr: null));

        Assert.False(result.CanActivate);
        Assert.Equal("system_place_cannot_be_activated", result.Reason);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void New_activation_setting_is_the_inverse_of_the_legacy_setting(bool preserve, bool setActive)
    {
        var json = JsonSerializer.Serialize(new { type = "invite", preserve_status_on_activation = preserve });
        var result = Policy().Evaluate(Structure(json),
            new HashSet<uint> { ProgramCommandTags.ActivatePlace }, Place());
        Assert.True(result.CanActivate);
        Assert.Equal(setActive, result.SetActiveOnActivation);
    }

    [Theory]
    [InlineData("null", "activity_configuration_invalid")]
    [InlineData("{\"type\":\"marketing\"}", "activity_configuration_invalid")]
    [InlineData("{\"type\":\"invite\",\"when_inactive\":{\"allow_inviting_with_places\":true}}", "activity_rules_not_supported_yet")]
    public void Invalid_or_not_yet_implemented_rules_return_a_business_error(string json, string reason)
    {
        var result = Policy().Evaluate(Structure(json),
            new HashSet<uint> { ProgramCommandTags.ActivatePlace }, Place());
        Assert.False(result.CanActivate);
        Assert.Equal(reason, result.Reason);
    }

    private static ActivatePlacePolicy Policy() => new(
        new Structures(),
        new Places(),
        new Commands());

    private static StructureResponse Structure(string? activityJson) => new()
    {
        StructureNumber = 0,
        Activity = activityJson is null
            ? null
            : JsonSerializer.Deserialize<JsonElement>(activityJson)
    };

    private static PlaceResponse Place(
        string? profileAddr = "profile",
        long? activatedAt = null) => new()
    {
        ProfileAddr = profileAddr,
        ActivatedAt = activatedAt
    };

    private sealed class Structures : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Places : PlaceQueriesStub
    {
    }

    private sealed class Commands : IProgramCommandQueries
    {
        public Task<ProgramCommandConfiguration> GetConfigurationAsync(string marketingAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
