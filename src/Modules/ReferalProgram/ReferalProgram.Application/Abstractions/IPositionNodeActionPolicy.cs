namespace ReferalProgram.Application.Abstractions;

public sealed record PositionNodeActionContext(
    byte Width,
    string ViewerRootMp,
    string[] ViewerLockMps,
    bool ViewerOwnsProfile,
    IReadOnlySet<uint> AvailableCommandTags,
    BuyPlaceDecision BuyDecision,
    bool IncludePosition);

public sealed record PositionNodeActions(
    bool IsLocked,
    bool IsLock,
    bool CanBuy,
    bool CanLock,
    bool CanUnlock,
    uint? BuyCommandTag,
    bool IncludePosition);

public interface IPositionNodeActionPolicy
{
    PositionNodeActions Evaluate(
        PositionNodeActionContext context,
        PlaceResponse? parent,
        string mp,
        uint position);
}
