namespace ReferalProgram.Application.Services;

public sealed class PositionGroupSelector : IPositionGroupSelector
{
    public PositionGroupConfiguration Select(
        PositionAlgorithmConfiguration configuration,
        IReadOnlyDictionary<byte, long> placeCounts) =>
        configuration.Relation.ToLowerInvariant() switch
        {
            "relative" => SelectRelative(configuration.Groups, placeCounts),
            "absolute" => SelectAbsolute(configuration.Groups, placeCounts),
            _ => throw new InvalidOperationException(
                $"Unknown pos_algo relation '{configuration.Relation}'.")
        };

    private static PositionGroupConfiguration SelectRelative(
        IReadOnlyCollection<PositionGroupConfiguration> groups,
        IReadOnlyDictionary<byte, long> counts)
    {
        var totalWeight = groups.Sum(group => (double)group.Weight);
        var totalPlaces = groups.Sum(group => Count(group, counts));

        return groups
            .OrderByDescending(group =>
                group.Weight / totalWeight
                - (totalPlaces == 0 ? 0 : Count(group, counts) / (double)totalPlaces))
            .ThenBy(group => group.Id)
            .First();
    }

    private static PositionGroupConfiguration SelectAbsolute(
        IReadOnlyCollection<PositionGroupConfiguration> groups,
        IReadOnlyDictionary<byte, long> counts)
    {
        var completedRounds = groups.Min(group => Count(group, counts) / group.Weight);

        return groups
            .OrderByDescending(group =>
                checked((completedRounds + 1) * group.Weight) - Count(group, counts))
            .ThenBy(group => group.Id)
            .First();
    }

    private static long Count(
        PositionGroupConfiguration group,
        IReadOnlyDictionary<byte, long> counts) =>
        counts.GetValueOrDefault(checked((byte)group.Id));
}
