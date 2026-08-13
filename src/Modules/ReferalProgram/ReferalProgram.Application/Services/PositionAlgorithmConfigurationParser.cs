using System.Text.Json;

namespace ReferalProgram.Application.Services;

public sealed class PositionAlgorithmConfigurationParser
    : IPositionAlgorithmConfigurationParser
{
    public PositionAlgorithmConfiguration Parse(JsonElement json)
    {
        var configuration = json.Deserialize<PositionAlgorithmConfiguration>()
            ?? throw new InvalidOperationException("Structure pos_algo is empty or invalid.");

        if (configuration.Version != 1)
            throw new NotSupportedException(
                $"pos_algo version {configuration.Version} is not supported.");

        if (string.IsNullOrWhiteSpace(configuration.Root))
            throw new InvalidOperationException("pos_algo root is required.");

        if (string.IsNullOrWhiteSpace(configuration.Relation))
            throw new InvalidOperationException("pos_algo relation is required.");

        if (configuration.Groups.Count == 0)
            throw new InvalidOperationException("pos_algo must contain at least one group.");

        if (configuration.Groups.Select(group => group.Id).Distinct().Count()
            != configuration.Groups.Count)
        {
            throw new InvalidOperationException("pos_algo group IDs must be unique.");
        }

        foreach (var group in configuration.Groups)
        {
            if (group.Id is < byte.MinValue or > byte.MaxValue)
                throw new InvalidOperationException(
                    $"pos_algo group ID {group.Id} is outside the byte range.");

            if (string.IsNullOrWhiteSpace(group.Algorithm))
                throw new InvalidOperationException(
                    $"pos_algo group {group.Id} has no algorithm.");

            if (group.Weight <= 0)
                throw new InvalidOperationException(
                    $"pos_algo group {group.Id} must have a positive weight.");
        }

        return configuration;
    }
}
