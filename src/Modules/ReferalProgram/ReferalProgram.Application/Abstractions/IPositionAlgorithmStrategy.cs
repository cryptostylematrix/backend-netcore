namespace ReferalProgram.Application.Abstractions;

public sealed record PositionAlgorithmStrategyContext(
    string MarketingAddr,
    byte StructureNumber,
    byte Width,
    PlaceResponse Root,
    byte PosGroup,
    bool ProfiledPlacesPrioritized,
    byte DepthSpread,
    string[] RootProfileLockMps,
    uint? CutFactor = null,
    uint? ProfiledFrontierLimit = null)
{
    public bool IsLocked(string mp) => RootProfileLockMps.Any(lockMp =>
        mp.StartsWith(lockMp, StringComparison.Ordinal));
}

public interface IPositionAlgorithmStrategy
{
    string Name { get; }

    Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken);
}
