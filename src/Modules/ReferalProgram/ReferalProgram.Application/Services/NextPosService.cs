using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Services;

public sealed class NextPosService(
    IStructureQueries structureQueries,
    IPlaceQueries placeQueries) : INextPosService
{
    public async Task<NextPosResponse?> GetNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        CancellationToken ct)
    {
        var structure = await structureQueries.GetStructureAsync(
            marketingAddr,
            structureNumber,
            ct);

        if (structure is null)
            return null;

        var config = structure.PosAlgo.Deserialize<PosAlgoV1>()
            ?? throw new InvalidOperationException("Structure pos_algo is empty or invalid.");

        Validate(config);

        var counts = await placeQueries.GetPlaceCountsByPosGroupAsync(
            marketingAddr,
            structureNumber,
            ct);

        var group = SelectGroup(config, counts);

        PlaceResponse? root = config.Root.ToLowerInvariant() switch
        {
            "owner" => await placeQueries.GetRootPlaceAsync(marketingAddr, structureNumber, ct),
            "profile" => throw new NotImplementedException("The profile pos_algo root is not implemented."),
            _ => throw new InvalidOperationException($"Unknown pos_algo root '{config.Root}'.")
        };

        if (root is null)
            return null;

        switch (group.Algo.ToLowerInvariant())
        {
            case "chess":
                return await GetChessNextPosAsync(
                    marketingAddr,
                    structureNumber,
                    structure.Width,
                    root,
                    ct);
            case "radar":
                return await GetRadarNextPosAsync(
                    marketingAddr,
                    structureNumber,
                    structure.Width,
                    root,
                    ct);
            case "classic":
                throw new NotImplementedException("The classic position algorithm is not implemented.");
            default:
                throw new InvalidOperationException($"Unknown position algorithm '{group.Algo}'.");
        }

        throw new NotImplementedException(
            $"The '{group.Algo}' position algorithm for group {group.Id} is not implemented.");
    }

    private async Task<NextPosResponse?> GetChessNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        byte width,
        PlaceResponse root,
        CancellationToken ct)
    {
        if (width == 0)
            return null;

        var places = await placeQueries.GetUnfilledPlacesAtMinDepthAsync(
            marketingAddr,
            structureNumber,
            root.Mp,
            width,
            ct);

        for (uint filling = 0; filling < width; filling++)
        {
            foreach (var place in ChessOrder(places))
            {
                if (place.Filling != filling)
                    continue;

                var pos = checked(place.Filling + 1);
                return new NextPosResponse
                {
                    ProfileAddr = place.ProfileAddr,
                    PlaceNumber = place.PlaceNumber,
                    Pos = pos,
                    Mp = place.Mp + pos.ToString("X8")
                };
            }
        }

        return null;
    }

    private async Task<NextPosResponse?> GetRadarNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        byte width,
        PlaceResponse root,
        CancellationToken ct)
    {
        if (width == 0)
            return null;

        var place = await placeQueries.GetFirstActiveUnfilledPlaceAsync(
            marketingAddr,
            structureNumber,
            root.Mp,
            width,
            ct);

        if (place is null)
            return null;

        var pos = checked(place.Filling + 1);
        return new NextPosResponse
        {
            ProfileAddr = place.ProfileAddr,
            PlaceNumber = place.PlaceNumber,
            Pos = pos,
            Mp = place.Mp + pos.ToString("X8")
        };
    }

    private static IEnumerable<PlaceResponse> ChessOrder(IReadOnlyList<PlaceResponse> places)
    {
        var left = 0;
        var right = places.Count - 1;

        while (left <= right)
        {
            yield return places[left++];

            if (left <= right)
                yield return places[right--];
        }
    }

    private static PosGroupV1 SelectGroup(
        PosAlgoV1 config,
        IReadOnlyDictionary<byte, long> counts)
    {
        return config.Relation.ToLowerInvariant() switch
        {
            "relative" => SelectRelativeGroup(config.Groups, counts),
            "absolute" => SelectAbsoluteGroup(config.Groups, counts),
            _ => throw new InvalidOperationException(
                $"Unknown pos_algo relation '{config.Relation}'.")
        };
    }

    private static PosGroupV1 SelectRelativeGroup(
        IReadOnlyCollection<PosGroupV1> groups,
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

    private static PosGroupV1 SelectAbsoluteGroup(
        IReadOnlyCollection<PosGroupV1> groups,
        IReadOnlyDictionary<byte, long> counts)
    {
        var completedRounds = groups.Min(group =>
            Count(group, counts) / group.Weight);

        return groups
            .OrderByDescending(group =>
                checked((completedRounds + 1) * group.Weight) - Count(group, counts))
            .ThenBy(group => group.Id)
            .First();
    }

    private static long Count(PosGroupV1 group, IReadOnlyDictionary<byte, long> counts) =>
        counts.GetValueOrDefault(checked((byte)group.Id));

    private static void Validate(PosAlgoV1 config)
    {
        if (config.Version != 1)
            throw new NotSupportedException($"pos_algo version {config.Version} is not supported.");

        if (string.IsNullOrWhiteSpace(config.Root))
            throw new InvalidOperationException("pos_algo root is required.");

        if (string.IsNullOrWhiteSpace(config.Relation))
            throw new InvalidOperationException("pos_algo relation is required.");

        if (config.Groups.Count == 0)
            throw new InvalidOperationException("pos_algo must contain at least one group.");

        if (config.Groups.Select(group => group.Id).Distinct().Count() != config.Groups.Count)
            throw new InvalidOperationException("pos_algo group IDs must be unique.");

        foreach (var group in config.Groups)
        {
            if (group.Id is < byte.MinValue or > byte.MaxValue)
                throw new InvalidOperationException($"pos_algo group ID {group.Id} is outside the byte range.");

            if (string.IsNullOrWhiteSpace(group.Algo))
                throw new InvalidOperationException($"pos_algo group {group.Id} has no algorithm.");

            if (group.Weight <= 0)
                throw new InvalidOperationException($"pos_algo group {group.Id} must have a positive weight.");
        }
    }

    private sealed class PosAlgoV1
    {
        [JsonPropertyName("v")]
        public int Version { get; init; }

        [JsonPropertyName("root")]
        public string Root { get; init; } = null!;

        [JsonPropertyName("groups")]
        public IReadOnlyCollection<PosGroupV1> Groups { get; init; } = [];

        [JsonPropertyName("relation")]
        public string Relation { get; init; } = null!;
    }

    private sealed class PosGroupV1
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("algo")]
        public string Algo { get; init; } = null!;

        [JsonPropertyName("weight")]
        public required int Weight { get; init; }
    }
}
