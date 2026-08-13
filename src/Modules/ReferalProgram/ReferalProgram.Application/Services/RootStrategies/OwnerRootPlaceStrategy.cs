namespace ReferalProgram.Application.Services.RootStrategies;

public sealed class OwnerRootPlaceStrategy(IPlaceQueries placeQueries)
    : IRootPlaceStrategy
{
    public string Name => "owner";

    public Task<PlaceResponse?> ResolveAsync(
        RootPlaceStrategyContext context,
        CancellationToken cancellationToken) =>
        placeQueries.GetRootPlaceAsync(
            context.MarketingAddr,
            context.StructureNumber,
            cancellationToken);
}
