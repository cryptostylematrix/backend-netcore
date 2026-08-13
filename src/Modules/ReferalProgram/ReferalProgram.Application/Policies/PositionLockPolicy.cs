namespace ReferalProgram.Application.Policies;

public sealed class PositionLockPolicy : IPositionLockPolicy
{
    public PositionLockDecision Evaluate(
        PositionLockContext context,
        PlaceResponse? parent,
        string mp,
        uint position)
    {
        var isLock = context.ViewerLockMps.Contains(mp, StringComparer.Ordinal);
        var isLocked = context.ViewerLockMps.Any(lockMp =>
            mp.StartsWith(lockMp, StringComparison.Ordinal));

        if (!context.ViewerOwnsProfile)
            return Decision(isLocked, isLock, reason: "viewer_is_not_profile_owner");

        if (!mp.StartsWith(context.ViewerRootMp, StringComparison.Ordinal))
            return Decision(isLocked, isLock, reason: "position_is_outside_viewer_root");

        var canUnlock = context.CanIssueUnlockCommand && isLock;
        if (isLocked)
        {
            return new PositionLockDecision(
                ViewerAuthorized: true,
                isLocked,
                isLock,
                CanLock: false,
                canUnlock,
                isLock ? null : "position_is_inside_locked_subtree");
        }

        if (!context.CanIssueLockCommand)
            return Decision(isLocked, isLock, canUnlock, "lock_command_not_configured");

        if (string.Equals(mp, context.ViewerRootMp, StringComparison.Ordinal))
            return Decision(isLocked, isLock, canUnlock, "viewer_root_cannot_be_locked");

        if (parent is null)
            return Decision(isLocked, isLock, canUnlock, "parent_place_not_found");

        if (parent.Filling == 0)
            return Decision(isLocked, isLock, canUnlock, "parent_has_no_filled_positions");

        if (string.IsNullOrWhiteSpace(parent.ProfileAddr)
            || string.IsNullOrWhiteSpace(parent.ProfileLogin))
        {
            return Decision(isLocked, isLock, canUnlock, "system_place_cannot_be_locked");
        }

        if (context.Width > 0 && position > context.Width)
            return Decision(isLocked, isLock, canUnlock, "position_is_outside_structure_width");

        var directLocks = context.ViewerLockMps
            .Where(lockMp => IsDirectChild(parent.Mp, lockMp))
            .ToArray();
        if (directLocks.Any(lockMp => !string.Equals(lockMp, mp, StringComparison.Ordinal)))
            return Decision(isLocked, isLock, canUnlock, "sibling_position_is_already_locked");

        if (context.Width > 0 && (long)context.Width - directLocks.Length <= 1)
            return Decision(isLocked, isLock, canUnlock, "final_available_position_cannot_be_locked");

        return new PositionLockDecision(
            ViewerAuthorized: true,
            isLocked,
            isLock,
            CanLock: true,
            canUnlock,
            Reason: null);
    }

    private static PositionLockDecision Decision(
        bool isLocked,
        bool isLock,
        bool canUnlock = false,
        string? reason = null) =>
        new(
            ViewerAuthorized: reason != "viewer_is_not_profile_owner",
            isLocked,
            isLock,
            CanLock: false,
            canUnlock,
            reason);

    private static bool IsDirectChild(string parentMp, string candidateMp) =>
        candidateMp.Length == parentMp.Length + 8
        && candidateMp.StartsWith(parentMp, StringComparison.Ordinal);
}
