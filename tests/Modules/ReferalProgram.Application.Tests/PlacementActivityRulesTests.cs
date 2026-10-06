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
        var rules = new PlacementActivityRules("child", "inviter", false, false, false, false);
        Assert.Equal(own, rules.IsOwnChild(parent));
    }

    [Fact]
    public void Own_permission_never_grants_spillover_and_spillover_permission_never_grants_own_children()
    {
        var ownOnly = new PlacementActivityRules("child", "inviter", true, false, true, true);
        var spillOnly = ownOnly with { AllowOwnChildren = false, AllowInactiveSpillover = true };
        var own = new PlaceResponse { ProfileAddr = "inviter", IsActive = false };
        var other = new PlaceResponse { ProfileAddr = "other", IsActive = false };
        Assert.True(ownOnly.Allows(own, false));
        Assert.False(ownOnly.Allows(other, true));
        Assert.False(spillOnly.Allows(own, true));
        Assert.True(spillOnly.Allows(other, true));
        Assert.False(spillOnly.Allows(other, false));
    }

    [Fact]
    public void Inactive_invite_blocks_spillover_even_to_an_active_place_but_not_own_children()
    {
        var rules = new PlacementActivityRules("child", "inviter", false, false, true, true);
        Assert.False(rules.Allows(new PlaceResponse { ProfileAddr = "other", IsActive = true }, false));
        Assert.True(rules.Allows(new PlaceResponse { ProfileAddr = "inviter", IsActive = true }, false));
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
            Activity = new PlacementActivityRules("child", "inviter", false, false, true, true),
            ActiveInviteProfiles = inviteActive ? new HashSet<string> { "other" } : new HashSet<string>()
        };
        var parent = new PlaceResponse { ProfileAddr = "other", IsActive = true, Filling = 0 };
        Assert.Equal(expected, policy.EvaluatePosition(decision, parent, "root00000001", 1, false).CanBuy);
        Assert.True(policy.EvaluatePosition(decision with { Activity = decision.Activity with { CheckManualPlacement = false } },
            parent, "root00000001", 1, false).CanBuy);
    }

    [Fact]
    public void Manual_placement_preserves_legacy_exception_until_explicitly_enabled()
    {
        var parent = new PlaceResponse { ProfileAddr = "other", IsActive = false };
        var rules = new PlacementActivityRules("child", "inviter", false, false, true, false);
        Assert.True(rules.Allows(parent, false, manual: true));
        Assert.False((rules with { CheckManualPlacement = true }).Allows(parent, false, manual: true));
    }
}
