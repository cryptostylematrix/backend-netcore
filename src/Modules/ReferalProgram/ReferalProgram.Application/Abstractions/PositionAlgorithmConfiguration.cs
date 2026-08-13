using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Abstractions;

public sealed class PositionAlgorithmConfiguration
{
    [JsonPropertyName("v")]
    public int Version { get; init; }

    [JsonPropertyName("root")]
    public string Root { get; init; } = null!;

    [JsonPropertyName("groups")]
    public IReadOnlyCollection<PositionGroupConfiguration> Groups { get; init; } = [];

    [JsonPropertyName("relation")]
    public string Relation { get; init; } = null!;
}

public sealed class PositionGroupConfiguration
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("algo")]
    public string Algorithm { get; init; } = null!;

    [JsonPropertyName("weight")]
    public required int Weight { get; init; }

    [JsonPropertyName("profiled_places_prioritized")]
    public bool ProfiledPlacesPrioritized { get; init; } = true;

    [JsonPropertyName("depth_spread")]
    public byte DepthSpread { get; init; } = 1;
}

public interface IPositionAlgorithmConfigurationParser
{
    PositionAlgorithmConfiguration Parse(JsonElement json);
}

public interface IPositionGroupSelector
{
    PositionGroupConfiguration Select(
        PositionAlgorithmConfiguration configuration,
        IReadOnlyDictionary<byte, long> placeCounts);
}
