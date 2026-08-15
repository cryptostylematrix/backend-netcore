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
    public bool HasPlacesInBuyFirstPlaceStructures { get; init; }
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

public sealed record ProgramCommandConfiguration(
    IReadOnlyDictionary<byte, IReadOnlySet<uint>> CommandTagsByStructure)
{
    public IReadOnlySet<uint> GetAvailableCommandTags(byte structureNumber) =>
        CommandTagsByStructure.TryGetValue(structureNumber, out var tags)
            ? tags
            : new HashSet<uint>();

    public IReadOnlySet<byte> GetStructureNumbers(uint commandTag) =>
        CommandTagsByStructure
            .Where(entry => entry.Value.Contains(commandTag))
            .Select(entry => entry.Key)
            .ToHashSet();
}

public interface IProgramCommandQueries
{
    Task<ProgramCommandConfiguration> GetConfigurationAsync(
        string marketingAddr,
        CancellationToken cancellationToken);
}
