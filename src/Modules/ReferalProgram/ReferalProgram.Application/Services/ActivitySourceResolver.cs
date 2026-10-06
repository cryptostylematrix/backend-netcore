using System.Text.Json;

namespace ReferalProgram.Application.Services;

// Resolves one direct source; never follows another structure's activity_source.
public static class ActivitySourceResolver
{
    public static async Task<byte?> ResolveAsync(StructureResponse? structure, IPlaceQueries queries,
        CancellationToken cancellationToken)
    {
        if (structure?.Activity is not { ValueKind: JsonValueKind.Object } json
            || !json.TryGetProperty("activity_source", out _))
            return null;
        var settings = ActivitySettings.Parse(json, structure.StructureNumber);
        return settings.ActivitySource switch
        {
            "place" => null,
            "invite" => 0,
            "group_root" => !string.IsNullOrWhiteSpace(structure.Group)
                ? await queries.GetGroupRootStructureAsync(structure.MarketingAddr, structure.StructureNumber, cancellationToken)
                    ?? throw new JsonException("The activity group has no root structure.")
                : throw new JsonException("group_root activity requires a structure group."),
            _ => throw new JsonException("Unknown activity source.")
        };
    }

    public static async Task<bool> IsActiveAsync(PlaceResponse place, byte? sourceStructure,
        IPlaceQueries queries, IDictionary<string, bool> cache, CancellationToken cancellationToken)
    {
        // System places have no profile source and retain their own status.
        if (sourceStructure is null || place.ProfileAddr is null)
            return place.IsActive;
        if (!cache.TryGetValue(place.ProfileAddr, out var active))
        {
            var source = await queries.GetPlaceAsync(place.MarketingAddr, sourceStructure.Value,
                place.ProfileAddr, 1, cancellationToken);
            active = source?.IsActive == true;
            cache[place.ProfileAddr] = active;
        }
        return active;
    }
}
