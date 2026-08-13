namespace ReferalProgram.Application.Policies;

public sealed class PositionNodeActionPolicy(
    IPositionLockPolicy lockPolicy,
    IBuyPlacePolicy buyPlacePolicy)
    : IPositionNodeActionPolicy
{
    public PositionNodeActions Evaluate(
        PositionNodeActionContext context,
        PlaceResponse? parent,
        string mp,
        uint position)
    {
        var lockDecision = lockPolicy.Evaluate(
            new PositionLockContext(
                context.Width,
                context.ViewerRootMp,
                context.ViewerLockMps,
                context.ViewerOwnsProfile,
                context.AvailableCommandTags.Contains(ProgramCommandTags.LockPosition),
                context.AvailableCommandTags.Contains(ProgramCommandTags.UnlockPosition)),
            parent,
            mp,
            position);
        var buyPosition = buyPlacePolicy.EvaluatePosition(
            context.BuyDecision,
            parent,
            mp,
            position,
            lockDecision.IsLocked);

        return new PositionNodeActions(
            lockDecision.IsLocked,
            lockDecision.IsLock,
            buyPosition.CanBuy,
            lockDecision.CanLock,
            lockDecision.CanUnlock,
            buyPosition.CommandTag,
            buyPosition.CanBuy && context.IncludePosition);
    }

}
