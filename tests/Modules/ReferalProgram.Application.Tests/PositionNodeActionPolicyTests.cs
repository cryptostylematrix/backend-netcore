using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class PositionNodeActionPolicyTests
{
    private readonly PositionNodeActionPolicy policy = new(
        new PositionLockPolicy(),
        new BuyPolicy());

    [Fact]
    public void Only_the_authoritative_next_position_can_be_bought()
    {
        var context = Context(nextMp: "ROOT00000002");
        var parent = Parent();

        var next = policy.Evaluate(context, parent, "ROOT00000002", 2);
        var other = policy.Evaluate(context, parent, "ROOT00000003", 3);

        Assert.True(next.CanBuy);
        Assert.Equal(ProgramCommandTags.BuyPlace, next.BuyCommandTag);
        Assert.False(other.CanBuy);
        Assert.Null(other.BuyCommandTag);
    }

    [Fact]
    public void Viewer_wallet_does_not_need_to_own_profile_to_buy_for_it()
    {
        var context = Context(nextMp: "ROOT00000002") with
        {
            ViewerOwnsProfile = false
        };

        var actions = policy.Evaluate(
            context,
            Parent(),
            "ROOT00000002",
            2);

        Assert.True(actions.CanBuy);
        Assert.False(actions.CanLock);
        Assert.False(actions.CanUnlock);
    }

    [Fact]
    public void Locked_subtree_cannot_be_bought_or_locked()
    {
        var context = Context(
            nextMp: "ROOT0000000200000001",
            locks: ["ROOT00000002"]);

        var actions = policy.Evaluate(
            context,
            Parent(mp: "ROOT00000002"),
            "ROOT0000000200000001",
            1);

        Assert.True(actions.IsLocked);
        Assert.False(actions.IsLock);
        Assert.False(actions.CanBuy);
        Assert.False(actions.CanLock);
    }

    [Fact]
    public void Position_can_be_locked_when_parent_has_a_child_and_no_sibling_lock()
    {
        var context = Context(nextMp: "other", width: 2);

        var actions = policy.Evaluate(context, Parent(filling: 1), "ROOT00000002", 2);

        Assert.True(actions.CanLock);
    }

    [Fact]
    public void Position_cannot_be_locked_when_a_sibling_is_already_locked()
    {
        var context = Context(
            nextMp: "other",
            width: 2,
            locks: ["ROOT00000001"]);

        var actions = policy.Evaluate(context, Parent(filling: 1), "ROOT00000002", 2);

        Assert.False(actions.CanLock);
    }

    [Fact]
    public void Viewer_can_unlock_only_the_exact_lock()
    {
        var context = Context(nextMp: "other", locks: ["ROOT00000002"]);

        var exact = policy.Evaluate(context, Parent(), "ROOT00000002", 2);
        var descendant = policy.Evaluate(
            context,
            Parent(mp: "ROOT00000002"),
            "ROOT0000000200000001",
            1);

        Assert.True(exact.CanUnlock);
        Assert.False(descendant.CanUnlock);
    }

    private static PositionNodeActionContext Context(
        string nextMp,
        byte width = 3,
        string[]? locks = null) =>
        new(
            width,
            "ROOT",
            locks ?? [],
            ViewerOwnsProfile: true,
            new HashSet<uint>
            {
                ProgramCommandTags.BuyPlace,
                ProgramCommandTags.LockPosition,
                ProgramCommandTags.UnlockPosition
            },
            new BuyPlaceDecision(
                true,
                BuyPlaceKind.Regular,
                ProgramCommandTags.BuyPlace,
                true,
                new NextPosResponse { Mp = nextMp },
                null),
            IncludePosition: true);

    private static PlaceResponse Parent(string mp = "ROOT", uint filling = 1) =>
        new()
        {
            Mp = mp,
            ProfileAddr = "profile",
            ProfileLogin = "login",
            IsActive = true,
            Filling = filling
        };

    private sealed class BuyPolicy : IBuyPlacePolicy
    {
        public BuyPositionDecision EvaluatePosition(
            BuyPlaceDecision decision,
            PlaceResponse? parent,
            string mp,
            uint position,
            bool isLocked)
        {
            var canBuy = decision.CanBuy
                && !isLocked
                && string.Equals(mp, decision.Position?.Mp, StringComparison.Ordinal);
            return new BuyPositionDecision(
                canBuy,
                canBuy ? decision.CommandTag : null);
        }

        public Task<BuyPlaceDecision> EvaluateAsync(
            string marketingAddr,
            byte structureNumber,
            string profileAddr,
            RequestedPosition? requestedPosition,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

    }
}
