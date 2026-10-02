using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntegrationRequests.Scheduling;

public sealed record ScheduleTarget(
    [property: JsonPropertyName("module")] string Module,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("resource_type")] string ResourceType,
    [property: JsonPropertyName("resource_id")] string ResourceId);

public sealed record PublicScheduledAction(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("target")] ScheduleTarget Target);

// Implemented by the command owner. Only explicitly public commands may be described.
public interface IPublicTaskCommandDescriptor
{
    string Module { get; }
    // JSON containment filter for persisted command envelopes, passed as a SQL parameter.
    string BuildCommandFilter(string scope);
    PublicScheduledAction? Describe(JsonElement command);
}
