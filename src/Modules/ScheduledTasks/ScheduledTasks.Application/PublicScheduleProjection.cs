using System.Text.Json;
using IntegrationRequests.Scheduling;
using ScheduledTasks.Application.Abstractions;
using ScheduledTasks.Core.TaskAggregate;

namespace ScheduledTasks.Application;

public static class PublicScheduleProjection
{
    public static PublicSchedule? Create(ScheduledTask row, ScheduleFilter filter, IPublicTaskCommandDescriptor descriptor)
    {
        using var document = JsonDocument.Parse(row.Commands);
        var actions = new List<PublicScheduledAction>();
        foreach (var command in document.RootElement.EnumerateArray())
        {
            var action = descriptor.Describe(command);
            if (action is not null && action.Target.Module == filter.Module
                && action.Target.Scope == filter.Scope && action.Target.ResourceType == filter.ResourceType)
                actions.Add(action);
        }
        if (actions.Count == 0) return null;
        using var schedule = row.Schedule is null ? null : JsonDocument.Parse(row.Schedule);
        var value = schedule?.RootElement ?? default;
        return new(row.Id, row.ExecuteAtUtc, row.Status.ToString().ToLowerInvariant(),
            Text(value, "type"), Text(value, "unit"),
            Number(value, "interval") ?? Number(value, "value"),
            Number(value, "dayOfMonth"), Text(value, "timeUtc"), actions);
    }

    private static string? Text(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item)
            && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    private static int? Number(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item)
            && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var number) ? number : null;
}
