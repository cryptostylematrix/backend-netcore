using System.Globalization;
using System.Text.Json;

namespace ScheduledTasks.Core.Scheduling;

public sealed class TaskScheduleCalculator
{
    public DateTimeOffset? GetNextExecution(
        string? schedule,
        DateTimeOffset previousExecutionUtc,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(schedule))
            return null;

        using var document = JsonDocument.Parse(schedule);
        var root = document.RootElement;
        var type = RequiredString(root, "type");

        return type switch
        {
            "interval" => NextInterval(root, previousExecutionUtc, nowUtc),
            "calendar" => NextCalendar(root, previousExecutionUtc, nowUtc),
            _ => throw new FormatException($"Unsupported schedule type '{type}'.")
        };
    }

    private static DateTimeOffset NextInterval(
        JsonElement root,
        DateTimeOffset previous,
        DateTimeOffset now)
    {
        var value = RequiredPositiveInt(root, "value");
        var unit = RequiredString(root, "unit");
        var interval = unit switch
        {
            "seconds" => TimeSpan.FromSeconds(value),
            "minutes" => TimeSpan.FromMinutes(value),
            "hours" => TimeSpan.FromHours(value),
            "days" => TimeSpan.FromDays(value),
            "weeks" => TimeSpan.FromDays(checked(value * 7)),
            _ => throw new FormatException($"Unsupported interval unit '{unit}'.")
        };

        if (previous > now)
            return previous;

        var elapsedTicks = (now - previous).Ticks;
        var periods = checked(elapsedTicks / interval.Ticks + 1);
        return previous.AddTicks(checked(periods * interval.Ticks));
    }

    private static DateTimeOffset NextCalendar(
        JsonElement root,
        DateTimeOffset previous,
        DateTimeOffset now)
    {
        var unit = RequiredString(root, "unit");
        if (unit != "months")
            throw new FormatException($"Unsupported calendar unit '{unit}'.");

        var interval = RequiredPositiveInt(root, "interval");
        var dayOfMonth = RequiredPositiveInt(root, "dayOfMonth");
        if (dayOfMonth > 31)
            throw new FormatException("dayOfMonth cannot be greater than 31.");

        var time = root.TryGetProperty("timeUtc", out var timeElement)
            ? TimeOnly.ParseExact(
                timeElement.GetString()!,
                "HH:mm:ss",
                CultureInfo.InvariantCulture)
            : TimeOnly.MinValue;

        var month = new DateTimeOffset(
            previous.Year,
            previous.Month,
            1,
            0,
            0,
            0,
            TimeSpan.Zero).AddMonths(interval);

        while (true)
        {
            var day = Math.Min(dayOfMonth, DateTime.DaysInMonth(month.Year, month.Month));
            var candidate = new DateTimeOffset(
                month.Year,
                month.Month,
                day,
                time.Hour,
                time.Minute,
                time.Second,
                TimeSpan.Zero);
            if (candidate > now)
                return candidate;

            month = month.AddMonths(interval);
        }
    }

    private static string RequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var element)
            || element.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(element.GetString()))
        {
            throw new FormatException($"Schedule must contain a non-empty '{name}'.");
        }

        return element.GetString()!;
    }

    private static int RequiredPositiveInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var element)
            || !element.TryGetInt32(out var value)
            || value <= 0)
        {
            throw new FormatException($"Schedule '{name}' must be a positive integer.");
        }

        return value;
    }
}
