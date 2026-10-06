using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Abstractions;

public abstract class ActivitySettings
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("preserve_status_on_activation")]
    public bool PreserveStatusOnActivation { get; init; }

    // Enabled restrictions are rejected until their consumers are implemented.
    public abstract bool HasPendingRules();

    public static ActivitySettings Parse(JsonElement json, byte structureNumber)
    {
        if (json.ValueKind != JsonValueKind.Object)
            throw new JsonException("Activity must be an object.");

        if (!json.TryGetProperty("type", out var type))
        {
            if (json.TryGetProperty("preserve_status_on_activation", out _)
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

        return structureNumber == 0
            ? json.Deserialize<InviteActivitySettings>()!
            : json.Deserialize<MarketingActivitySettings>()!;
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
    [JsonPropertyName("when_inactive")]
    public InactiveInviteSettings WhenInactive { get; init; } = new();

    public override bool HasPendingRules() => WhenInactive.AllowAsBonusRecipient || WhenInactive.AllowAsCloneRecipient
        || WhenInactive.KeepOnCompression;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MarketingActivitySettings : ActivitySettings
{
    [JsonPropertyName("when_inactive")]
    public InactiveMarketingSettings WhenInactive { get; init; } = new();

    [JsonPropertyName("spillover")]
    public SpilloverActivitySettings Spillover { get; init; } = new();

    public override bool HasPendingRules() => WhenInactive.AllowAsBonusRecipient
        || WhenInactive.AllowAsCloneRecipient || WhenInactive.KeepOnCompression;
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

    [JsonPropertyName("require_active_invite")]
    public bool RequireActiveInvite { get; init; }
}
