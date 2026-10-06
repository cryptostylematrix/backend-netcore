using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Abstractions;

public abstract class ActivitySettings
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("preserve_status_on_activation")]
    public bool PreserveStatusOnActivation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("activity_source")]
    public string? ActivitySource { get; init; }

    public static ActivitySettings Parse(JsonElement json, byte structureNumber)
    {
        if (json.ValueKind != JsonValueKind.Object)
            throw new JsonException("Activity must be an object.");

        if (!json.TryGetProperty("type", out var type))
        {
            if (json.TryGetProperty("activity_source", out _)
                || json.TryGetProperty("require_marketing_place_to_invite", out _)
                || json.TryGetProperty("preserve_status_on_activation", out _)
                || json.TryGetProperty("when_inactive", out _)
                || json.TryGetProperty("spillover", out _))
                throw new JsonException("New activity settings require a type.");

            // Preserve legacy deserialization, including ignored retired settings.
            var legacy = json.Deserialize<ActivityConfiguration>()!;
            return structureNumber == 0
                ? new InviteActivitySettings { Type = "invite", PreserveStatusOnActivation = !legacy.SetActiveOnActivation }
                : new MarketingActivitySettings { Type = "marketing", PreserveStatusOnActivation = !legacy.SetActiveOnActivation };
        }

        ValidateObject(json);
        var expectedType = structureNumber == 0 ? "invite" : "marketing";
        if (type.ValueKind != JsonValueKind.String || type.GetString() != expectedType)
            throw new JsonException("Activity type does not match the structure.");

        if (json.TryGetProperty("activity_source", out var source))
        {
            if (source.ValueKind != JsonValueKind.String || source.GetString() is not ("place" or "invite" or "group_root"))
                throw new JsonException("activity_source must be place, invite or group_root.");
            if (json.TryGetProperty("spillover", out _))
                throw new JsonException("activity_source cannot be mixed with legacy spillover settings.");
        }
        else if (json.TryGetProperty("when_inactive", out var inactive)
            && inactive.TryGetProperty("allow_spillover_children", out _))
            throw new JsonException("allow_spillover_children requires activity_source.");

        return structureNumber == 0
            ? json.Deserialize<InviteActivitySettings>()!
            : json.Deserialize<MarketingActivitySettings>()!;
    }

    // Legacy JSON never governed recipient eligibility or compression.
    public static InactiveRecipientSettings? ParseRecipientRules(JsonElement? json, byte structureNumber)
    {
        if (json is not { ValueKind: JsonValueKind.Object } value
            || !(value.TryGetProperty("activity_source", out _)
                || value.TryGetProperty("type", out _)
                || value.TryGetProperty("when_inactive", out _)
                || value.TryGetProperty("spillover", out _)
                || value.TryGetProperty("preserve_status_on_activation", out _)))
            return null;

        return Parse(value, structureNumber) switch
        {
            InviteActivitySettings invite => invite.WhenInactive,
            MarketingActivitySettings marketing => marketing.WhenInactive,
            _ => throw new InvalidOperationException("Unknown activity settings type.")
        };
    }

    private static void ValidateObject(JsonElement json)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in json.EnumerateObject())
        {
            if (!names.Add(property.Name))
                throw new JsonException("Duplicate activity property.");
            if (property.Name is "when_inactive" or "spillover")
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                    throw new JsonException("Activity settings block must be an object.");
                ValidateObject(property.Value);
            }
        }
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class InviteActivitySettings : ActivitySettings
{
    [JsonPropertyName("require_marketing_place_to_invite")]
    public bool RequireMarketingPlaceToInvite { get; init; }

    [JsonPropertyName("when_inactive")]
    public InactiveInviteSettings WhenInactive { get; init; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MarketingActivitySettings : ActivitySettings
{
    [JsonPropertyName("when_inactive")]
    public InactiveMarketingSettings WhenInactive { get; init; } = new();

    [JsonPropertyName("spillover")]
    public SpilloverActivitySettings Spillover { get; init; } = new();
}

public abstract class InactiveRecipientSettings
{
    [JsonPropertyName("allow_as_bonus_recipient")]
    public bool AllowAsBonusRecipient { get; init; }

    [JsonPropertyName("allow_as_clone_recipient")]
    public bool AllowAsCloneRecipient { get; init; }

    [JsonPropertyName("keep_on_compression")]
    public bool KeepOnCompression { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class InactiveInviteSettings : InactiveRecipientSettings
{
    [JsonPropertyName("allow_inviting_without_places")]
    public bool AllowInvitingWithoutPlaces { get; init; }

    [JsonPropertyName("allow_inviting_with_places")]
    public bool AllowInvitingWithPlaces { get; init; }

    [JsonPropertyName("allow_as_fallback_root")]
    public bool AllowAsFallbackRoot { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class InactiveMarketingSettings : InactiveRecipientSettings
{
    [JsonPropertyName("allow_spillover_children")]
    public bool AllowSpilloverChildren { get; init; }

    [JsonPropertyName("allow_own_children")]
    public bool AllowOwnChildren { get; init; }

    [JsonPropertyName("check_manual_placement")]
    public bool CheckManualPlacement { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SpilloverActivitySettings
{
    [JsonPropertyName("allow_inactive_place")]
    public bool AllowInactivePlace { get; init; }

}
