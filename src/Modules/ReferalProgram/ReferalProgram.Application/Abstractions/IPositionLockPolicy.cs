namespace ReferalProgram.Application.Abstractions;

public sealed record PositionLockContext(
    byte Width,
    string ViewerRootMp,
    string[] ViewerLockMps,
    bool ViewerOwnsProfile,
    bool CanIssueLockCommand,
    bool CanIssueUnlockCommand);

public sealed record PositionLockDecision(
    bool ViewerAuthorized,
    bool IsLocked,
    bool IsLock,
    bool CanLock,
    bool CanUnlock,
    string? Reason);

public interface IPositionLockPolicy
{
    PositionLockDecision Evaluate(
        PositionLockContext context,
        PlaceResponse? parent,
        string mp,
        uint position);
}
