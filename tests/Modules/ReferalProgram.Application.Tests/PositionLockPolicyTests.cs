using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class PositionLockPolicyTests
{
    private readonly PositionLockPolicy _policy = new();

    [Fact]
    public void Non_owner_can_observe_locks_but_cannot_change_them()
    {
        var result = _policy.Evaluate(
            Context(["ROOT00000001"], ownsProfile: false),
            Parent(),
            "ROOT0000000100000001",
            1);

        Assert.True(result.IsLocked);
        Assert.False(result.ViewerAuthorized);
        Assert.False(result.CanLock);
        Assert.False(result.CanUnlock);
        Assert.Equal("viewer_is_not_profile_owner", result.Reason);
    }

    [Fact]
    public void Exact_lock_can_be_unlocked_but_not_locked_again()
    {
        const string mp = "ROOT00000001";

        var result = _policy.Evaluate(Context([mp]), Parent(), mp, 1);

        Assert.True(result.IsLock);
        Assert.True(result.IsLocked);
        Assert.True(result.CanUnlock);
        Assert.False(result.CanLock);
    }

    [Fact]
    public void Final_available_position_cannot_be_locked()
    {
        var result = _policy.Evaluate(
            Context(["ROOT00000001"], width: 2),
            Parent(filling: 1),
            "ROOT00000002",
            2);

        Assert.False(result.CanLock);
        Assert.Equal("final_available_position_cannot_be_locked", result.Reason);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0, 3, true)]
    [InlineData(1, 0, false)]
    [InlineData(2, 0, true)]
    [InlineData(2, 1, false)]
    [InlineData(4, 0, true)]
    [InlineData(4, 1, true)]
    [InlineData(4, 2, true)]
    [InlineData(4, 3, false)]
    public void Width_limits_direct_locks_to_width_minus_one(
        byte width, int lockCount, bool expectedCanLock)
    {
        var locks = Enumerable.Range(1, lockCount)
            .Select(position => $"ROOT{position:X8}")
            .ToArray();
        var nextPosition = (uint)lockCount + 1;

        var result = _policy.Evaluate(
            Context(locks, width: width),
            Parent(),
            $"ROOT{nextPosition:X8}",
            nextPosition);

        Assert.Equal(expectedCanLock, result.CanLock);
        Assert.Equal(expectedCanLock ? null : "final_available_position_cannot_be_locked", result.Reason);
    }

    [Fact]
    public void Locks_under_other_parents_do_not_reduce_available_width()
    {
        var result = _policy.Evaluate(
            Context(["ROOT0000000100000001", "OTHER00000001"], width: 2),
            Parent(),
            "ROOT00000002",
            2);

        Assert.True(result.CanLock);
    }

    [Fact]
    public void System_parent_cannot_have_a_position_locked()
    {
        var result = _policy.Evaluate(
            Context([]),
            Parent(profileAddr: null, profileLogin: null),
            "ROOT00000001",
            1);

        Assert.False(result.CanLock);
        Assert.Equal("system_place_cannot_be_locked", result.Reason);
    }

    private static PositionLockContext Context(
        IReadOnlyCollection<string> locks,
        bool ownsProfile = true,
        byte width = 3) => new(
            Width: width,
            ViewerRootMp: "ROOT",
            ViewerLockMps: locks.ToArray(),
            ViewerOwnsProfile: ownsProfile,
            CanIssueLockCommand: true,
            CanIssueUnlockCommand: true);

    private static PlaceResponse Parent(
        uint filling = 1,
        string? profileAddr = "profile",
        string? profileLogin = "login") => new()
    {
        Mp = "ROOT",
        Filling = filling,
        ProfileAddr = profileAddr,
        ProfileLogin = profileLogin
    };
}
