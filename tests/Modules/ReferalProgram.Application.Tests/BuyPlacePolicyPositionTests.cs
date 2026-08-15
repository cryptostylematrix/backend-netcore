using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class BuyPlacePolicyPositionTests
{
    private readonly BuyPlacePolicy policy = new(
        null!, null!, null!, null!, null!);

    [Fact]
    public void Owner_root_allows_only_the_calculated_next_position()
    {
        var decision = Decision(requireNext: true);
        var parent = Parent("ROOT");

        var next = policy.EvaluatePosition(
            decision,
            parent,
            "ROOT00000002",
            2,
            isLocked: false);
        var other = policy.EvaluatePosition(
            decision,
            parent,
            "ROOT00000003",
            3,
            isLocked: false);

        Assert.True(next.CanBuy);
        Assert.False(other.CanBuy);
    }

    [Fact]
    public void Profile_root_allows_any_unlocked_position_in_viewer_subtree()
    {
        var decision = Decision(requireNext: false);
        var parent = Parent("ROOT00000009", filling: 6);

        var inside = policy.EvaluatePosition(
            decision,
            parent,
            "ROOT0000000900000007",
            7,
            isLocked: false);
        var outside = policy.EvaluatePosition(
            decision,
            Parent("OTHER"),
            "OTHER00000001",
            1,
            isLocked: false);
        var locked = policy.EvaluatePosition(
            decision,
            parent,
            "ROOT0000000900000007",
            7,
            isLocked: true);
        var skippedSlot = policy.EvaluatePosition(
            decision,
            parent,
            "ROOT0000000900000008",
            8,
            isLocked: false);

        Assert.True(inside.CanBuy);
        Assert.False(outside.CanBuy);
        Assert.False(locked.CanBuy);
        Assert.False(skippedSlot.CanBuy);
    }

    [Fact]
    public void Candidate_for_profile_without_places_uses_buy_first()
    {
        var decision = Decision(requireNext: false) with
        {
            HasPlacesInBuyFirstPlaceStructures = false
        };

        var result = policy.EvaluatePosition(
            decision,
            Parent("ROOT"),
            "ROOT00000002",
            1,
            isLocked: false);

        Assert.Equal(ProgramCommandTags.BuyFirstPlace, result.CommandTag);
    }

    private static BuyPlaceDecision Decision(bool requireNext) =>
        new(
            CanBuy: true,
            BuyPlaceKind.Regular,
            ProgramCommandTags.BuyPlace,
            IncludePosition: !requireNext,
            new NextPosResponse { Mp = "ROOT00000002" },
            Reason: null)
        {
            RequireNextPosition = requireNext,
            ViewerRootMp = "ROOT",
            HasPlacesInBuyFirstPlaceStructures = true,
            AvailableCommandTags = new HashSet<uint>
            {
                ProgramCommandTags.BuyFirstPlace,
                ProgramCommandTags.BuyPlace
            }
        };

    private static PlaceResponse Parent(string mp, uint filling = 0) =>
        new() { Mp = mp, Filling = filling };
}
