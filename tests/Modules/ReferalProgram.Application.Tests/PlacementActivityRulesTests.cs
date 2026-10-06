using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class PlacementActivityRulesTests
{
    [Theory]
    [InlineData("child", true)]
    [InlineData("inviter", true)]
    [InlineData("other", false)]
    [InlineData(null, false)]
    public void Own_children_are_profile_places_or_personally_invited_profiles(string? parent, bool own)
    {
        var rules = new PlacementActivityRules("child", "inviter", false, false, false);
        Assert.Equal(own, rules.IsOwnChild(parent));
    }

    [Fact]
    public void Own_permission_never_grants_spillover_and_spillover_permission_never_grants_own_children()
    {
        var ownOnly = new PlacementActivityRules("child", "inviter", true, false, true);
        var spillOnly = ownOnly with { AllowOwnChildren = false, AllowInactiveSpillover = true };
        var own = new PlaceResponse { ProfileAddr = "inviter", IsActive = false };
        var other = new PlaceResponse { ProfileAddr = "other", IsActive = false };
        Assert.True(ownOnly.Allows(own, false));
        Assert.False(ownOnly.Allows(other, true));
        Assert.False(spillOnly.Allows(own, true));
        Assert.True(spillOnly.Allows(other, true));
        Assert.False((spillOnly with { AllowInactiveSpillover = false }).Allows(other, false));
    }

    [Fact]
    public void Inactive_invite_source_obeys_own_and_spillover_permissions()
    {
        var rules = new PlacementActivityRules("child", "inviter", false, false, true, 0);
        Assert.False(rules.Allows(new PlaceResponse { ProfileAddr = "other", IsActive = true }, false));
        Assert.False(rules.Allows(new PlaceResponse { ProfileAddr = "inviter", IsActive = true }, false));
        Assert.True((rules with { AllowOwnChildren = true }).Allows(new PlaceResponse { ProfileAddr = "inviter", IsActive = true }, false));
        Assert.True(rules.Allows(new PlaceResponse { ProfileAddr = null, IsActive = true }, false));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Tree_purchase_action_uses_the_batched_invite_status(bool inviteActive, bool expected)
    {
        var policy = new ReferalProgram.Application.Policies.BuyPlacePolicy(null!, null!, null!, null!, null!, null!);
        var decision = new BuyPlaceDecision(true, null, null, true, null, null)
        {
            ViewerRootMp = "root",
            AvailableCommandTags = new HashSet<uint> { ProgramCommandTags.BuyPlace },
            Activity = new PlacementActivityRules("child", "inviter", false, false, true, 0),
            ActiveSourceProfiles = inviteActive ? new HashSet<string> { "other" } : new HashSet<string>()
        };
        var parent = new PlaceResponse { ProfileAddr = "other", IsActive = true, Filling = 0 };
        Assert.Equal(expected, policy.EvaluatePosition(decision, parent, "root00000001", 1, false).CanBuy);
        Assert.True(policy.EvaluatePosition(decision with { Activity = decision.Activity with { CheckManualPlacement = false } },
            parent, "root00000001", 1, false).CanBuy);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Tree_action_uses_external_source_instead_of_own_status(bool ownActive, bool sourceActive)
    {
        var policy = new ReferalProgram.Application.Policies.BuyPlacePolicy(null!, null!, null!, null!, null!, null!);
        var decision = new BuyPlaceDecision(true, null, null, true, null, null)
        {
            ViewerRootMp = "root",
            AvailableCommandTags = new HashSet<uint> { ProgramCommandTags.BuyPlace },
            Activity = new("child", null, false, false, true, 1),
            ActiveSourceProfiles = sourceActive ? new HashSet<string> { "other" } : new HashSet<string>()
        };
        var parent = new PlaceResponse { ProfileAddr = "other", IsActive = ownActive, Filling = 0 };
        Assert.Equal(sourceActive, policy.EvaluatePosition(decision, parent, "root00000001", 1, false).CanBuy);
    }

    [Fact]
    public void Manual_placement_preserves_legacy_exception_until_explicitly_enabled()
    {
        var parent = new PlaceResponse { ProfileAddr = "other", IsActive = false };
        var rules = new PlacementActivityRules("child", "inviter", false, false, false, 0);
        Assert.True(rules.Allows(parent, false, manual: true));
        Assert.False((rules with { CheckManualPlacement = true }).Allows(parent, false, manual: true));
    }
}
