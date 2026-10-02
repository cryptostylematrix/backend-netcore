using System.Text.Json.Serialization;
using IntegrationRequests.Scheduling;

namespace ScheduledTasks.Application.Abstractions;

// Public projection: never include task errors, payloads, or unrelated commands.
public sealed record PublicSchedule(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("execute_at_utc")] DateTimeOffset? ExecuteAtUtc,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("schedule_type")] string? ScheduleType,
    [property: JsonPropertyName("unit")] string? Unit,
    [property: JsonPropertyName("interval")] int? Interval,
    [property: JsonPropertyName("day_of_month")] int? DayOfMonth,
    [property: JsonPropertyName("time_utc")] string? TimeUtc,
    [property: JsonPropertyName("actions")] IReadOnlyList<PublicScheduledAction> Actions);
public interface IPublicScheduleQueries
{
    Task<IReadOnlyList<PublicSchedule>> GetAsync(
        ScheduleFilter filter, CancellationToken cancellationToken);
}

public sealed record ScheduleFilter(string Module, string Scope, string ResourceType);
