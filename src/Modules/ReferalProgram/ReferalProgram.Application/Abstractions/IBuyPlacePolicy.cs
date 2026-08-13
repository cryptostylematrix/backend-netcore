namespace ReferalProgram.Application.Abstractions;

public sealed record RequestedPosition(
    byte StructureNumber,
    string? ParentProfileAddr,
    uint ParentPlaceNumber,
    uint Position);

public sealed record BuyPlaceDecision(
    bool CanBuy,
    BuyPlaceKind? Kind,
    uint? CommandTag,
    bool IncludePosition,
    NextPosResponse? Position,
    string? Reason)
{
    public bool RequireNextPosition { get; init; }
    public string? ViewerRootMp { get; init; }
    public long PlacesCount { get; init; }
    public string? TopPlaceMp { get; init; }
    public IReadOnlySet<uint> AvailableCommandTags { get; init; } = new HashSet<uint>();
}

public sealed record BuyPositionDecision(
    bool CanBuy,
    uint? CommandTag);

public interface IBuyPlacePolicy
{
    Task<BuyPlaceDecision> EvaluateAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        RequestedPosition? requestedPosition,
        CancellationToken cancellationToken);

    BuyPositionDecision EvaluatePosition(
        BuyPlaceDecision decision,
        PlaceResponse? parent,
        string mp,
        uint position,
        bool isLocked);
}

public interface IProgramCommandQueries
{
    Task<IReadOnlySet<uint>> GetAvailableCommandTagsAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);
}
