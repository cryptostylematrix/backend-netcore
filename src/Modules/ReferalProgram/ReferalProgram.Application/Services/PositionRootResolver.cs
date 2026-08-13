namespace ReferalProgram.Application.Services;

public sealed class PositionRootResolver(
    IEnumerable<IRootPlaceStrategy> strategies) : IPositionRootResolver
{
    private readonly IReadOnlyDictionary<string, IRootPlaceStrategy> _strategies =
        BuildRegistry(strategies);

    public Task<PlaceResponse?> ResolveAsync(
        string strategyName,
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        if (!_strategies.TryGetValue(strategyName, out var strategy))
            throw new InvalidOperationException($"Unknown pos_algo root '{strategyName}'.");

        return strategy.ResolveAsync(
            new RootPlaceStrategyContext(marketingAddr, structureNumber, profileAddr),
            cancellationToken);
    }

    private static IReadOnlyDictionary<string, IRootPlaceStrategy> BuildRegistry(
        IEnumerable<IRootPlaceStrategy> strategies)
    {
        var registry = new Dictionary<string, IRootPlaceStrategy>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var strategy in strategies)
        {
            if (string.IsNullOrWhiteSpace(strategy.Name))
                throw new InvalidOperationException("A root strategy has no name.");

            if (!registry.TryAdd(strategy.Name, strategy))
                throw new InvalidOperationException(
                    $"Root strategy '{strategy.Name}' is registered more than once.");
        }

        return registry;
    }
}
