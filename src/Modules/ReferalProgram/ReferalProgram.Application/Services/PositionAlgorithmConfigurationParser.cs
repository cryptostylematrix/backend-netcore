using System.Text.Json;

namespace ReferalProgram.Application.Services;

public sealed class PositionAlgorithmConfigurationParser
    : IPositionAlgorithmConfigurationParser
{
    public PositionAlgorithmConfiguration Parse(
        JsonElement json,
        PositionOperation? operation = null)
    {
        if (!json.TryGetProperty("v", out var versionProperty)
            || !versionProperty.TryGetInt32(out var version))
        {
            throw new InvalidOperationException("pos_algo version is missing or invalid.");
        }

        return version switch
        {
            1 => ParseVersionOne(json),
            2 => ParseVersionTwo(json, operation),
            _ => throw new NotSupportedException(
                $"pos_algo version {version} is not supported.")
        };
    }

    private static PositionAlgorithmConfiguration ParseVersionOne(JsonElement json)
    {
        var configuration = json.Deserialize<PositionAlgorithmConfiguration>()
            ?? throw new InvalidOperationException("Structure pos_algo is empty or invalid.");

        Validate(configuration, "pos_algo");
        return configuration;
    }

    private static PositionAlgorithmConfiguration ParseVersionTwo(
        JsonElement json,
        PositionOperation? operation)
    {
        var document = json.Deserialize<PositionAlgorithmConfigurationDocument>()
            ?? throw new InvalidOperationException("Structure pos_algo is empty or invalid.");

        if (document.Default is null)
            throw new InvalidOperationException("pos_algo default configuration is required.");

        Validate(document.Default, "pos_algo default");

        var configuredOperations = new HashSet<PositionOperation>();
        foreach (var (key, configuration) in document.Operations)
        {
            if (!PositionOperationNames.TryParse(key, out var configuredOperation))
            {
                throw new InvalidOperationException(
                    $"Unknown pos_algo operation '{key}'.");
            }

            if (!configuredOperations.Add(configuredOperation))
            {
                throw new InvalidOperationException(
                    $"pos_algo operation '{key}' is configured more than once.");
            }

            Validate(configuration, $"pos_algo operation '{key}'");
        }

        var operationConfiguration = operation is null
            ? null
            : document.Operations
                .FirstOrDefault(entry =>
                    PositionOperationNames.TryParse(
                        entry.Key,
                        out var configuredOperation)
                    && configuredOperation == operation.Value)
                .Value;
        if (operationConfiguration is not null)
        {
            return operationConfiguration with { Version = 2 };
        }

        return document.Default with { Version = 2 };
    }

    private static void Validate(
        PositionAlgorithmConfiguration configuration,
        string configurationName)
    {
        if (string.IsNullOrWhiteSpace(configuration.Root))
            throw new InvalidOperationException($"{configurationName} root is required.");

        if (string.IsNullOrWhiteSpace(configuration.Relation))
            throw new InvalidOperationException($"{configurationName} relation is required.");

        if (configuration.Groups.Count == 0)
            throw new InvalidOperationException(
                $"{configurationName} must contain at least one group.");

        if (configuration.Groups.Select(group => group.Id).Distinct().Count()
            != configuration.Groups.Count)
        {
            throw new InvalidOperationException(
                $"{configurationName} group IDs must be unique.");
        }

        foreach (var group in configuration.Groups)
        {
            if (group.Id is < byte.MinValue or > byte.MaxValue)
                throw new InvalidOperationException(
                    $"{configurationName} group ID {group.Id} is outside the byte range.");

            if (string.IsNullOrWhiteSpace(group.Algorithm))
                throw new InvalidOperationException(
                    $"{configurationName} group {group.Id} has no algorithm.");

            if (group.Weight <= 0)
                throw new InvalidOperationException(
                    $"{configurationName} group {group.Id} must have a positive weight.");

            if (group.Algorithm.Equals(
                    "trimmed_classic",
                    StringComparison.OrdinalIgnoreCase)
                && group.CutFactor is null or < 2)
            {
                throw new InvalidOperationException(
                    $"{configurationName} group {group.Id} must define a cut_factor of at least 2 for trimmed_classic.");
            }
        }
    }

    private sealed class PositionAlgorithmConfigurationDocument
    {
        [System.Text.Json.Serialization.JsonPropertyName("default")]
        public PositionAlgorithmConfiguration? Default { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("operations")]
        public IReadOnlyDictionary<string, PositionAlgorithmConfiguration> Operations
            { get; init; } = new Dictionary<string, PositionAlgorithmConfiguration>(
                StringComparer.OrdinalIgnoreCase);
    }
}
