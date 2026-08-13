namespace ReferalProgram.Application.Abstractions;

public sealed record PositionAlgorithmStrategyContext(
    string MarketingAddr,
    byte StructureNumber,
    byte Width,
    PlaceResponse Root,
    byte PosGroup,
    bool ProfiledPlacesPrioritized,
    byte DepthSpread);

public interface IPositionAlgorithmStrategy
{
    string Name { get; }

    Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken);
}
