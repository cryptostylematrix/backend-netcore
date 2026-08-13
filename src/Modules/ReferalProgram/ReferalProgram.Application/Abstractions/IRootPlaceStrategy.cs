namespace ReferalProgram.Application.Abstractions;

public sealed record RootPlaceStrategyContext(
    string MarketingAddr,
    byte StructureNumber,
    string? ProfileAddr);

public interface IRootPlaceStrategy
{
    string Name { get; }

    Task<PlaceResponse?> ResolveAsync(
        RootPlaceStrategyContext context,
        CancellationToken cancellationToken);
}
