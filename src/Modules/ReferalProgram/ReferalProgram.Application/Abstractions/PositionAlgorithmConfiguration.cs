using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Abstractions;

public enum PositionOperation
{
    BuyPlace,
    BuyFirstPlace,
    BuySystemPlace,
    CreateClone,
    CreateReinvest
}

public static class PositionOperationNames
{
    public const string BuyPlace = "buy_place";
    public const string BuyFirstPlace = "buy_first_place";
    public const string BuySystemPlace = "buy_system_place";
    public const string CreateClone = "create_clone";
    public const string CreateReinvest = "create_reinvest";

    public static IReadOnlyCollection<string> All { get; } =
    [
        BuyPlace,
        BuyFirstPlace,
        BuySystemPlace,
        CreateClone,
        CreateReinvest
    ];

    public static string ToConfigurationKey(this PositionOperation operation) =>
        operation switch
        {
            PositionOperation.BuyPlace => BuyPlace,
            PositionOperation.BuyFirstPlace => BuyFirstPlace,
            PositionOperation.BuySystemPlace => BuySystemPlace,
            PositionOperation.CreateClone => CreateClone,
            PositionOperation.CreateReinvest => CreateReinvest,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    public static bool TryParse(string? value, out PositionOperation operation)
    {
        var normalized = value?.Trim().Replace('-', '_').ToLowerInvariant();
        operation = normalized switch
        {
            BuyPlace => PositionOperation.BuyPlace,
            BuyFirstPlace => PositionOperation.BuyFirstPlace,
            BuySystemPlace => PositionOperation.BuySystemPlace,
            CreateClone => PositionOperation.CreateClone,
            CreateReinvest => PositionOperation.CreateReinvest,
            _ => default
        };

        return normalized is BuyPlace
            or BuyFirstPlace
            or BuySystemPlace
            or CreateClone
            or CreateReinvest;
    }
}

public sealed record PositionAlgorithmConfiguration
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
    PositionAlgorithmConfiguration Parse(
        JsonElement json,
        PositionOperation? operation = null);
}

public interface IPositionGroupSelector
{
    PositionGroupConfiguration Select(
        PositionAlgorithmConfiguration configuration,
        IReadOnlyDictionary<byte, long> placeCounts);
}
