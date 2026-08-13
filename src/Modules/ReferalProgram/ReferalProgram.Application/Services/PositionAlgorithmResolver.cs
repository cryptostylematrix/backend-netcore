namespace ReferalProgram.Application.Services;

public sealed class PositionAlgorithmResolver(
    IEnumerable<IPositionAlgorithmStrategy> strategies) : IPositionAlgorithmResolver
{
    private readonly IReadOnlyDictionary<string, IPositionAlgorithmStrategy> _strategies =
        BuildRegistry(strategies);

    public IPositionAlgorithmStrategy Resolve(string name) =>
        _strategies.TryGetValue(name, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"Unknown position algorithm '{name}'.");

    private static IReadOnlyDictionary<string, IPositionAlgorithmStrategy> BuildRegistry(
        IEnumerable<IPositionAlgorithmStrategy> strategies)
    {
        var registry = new Dictionary<string, IPositionAlgorithmStrategy>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var strategy in strategies)
        {
            if (string.IsNullOrWhiteSpace(strategy.Name))
                throw new InvalidOperationException("A position algorithm has no name.");

            if (!registry.TryAdd(strategy.Name, strategy))
                throw new InvalidOperationException(
                    $"Position algorithm '{strategy.Name}' is registered more than once.");
        }

        return registry;
    }
}
