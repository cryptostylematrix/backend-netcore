using ScheduledTasks.Application;
using ScheduledTasks.Core.Scheduling;

namespace ScheduledTasks.Application.Tests;

public sealed class TaskScheduleCalculatorTests
{
    private readonly TaskScheduleCalculator _calculator = new();

    [Fact]
    public void Interval_skips_missed_occurrences()
    {
        var previous = DateTimeOffset.Parse("2030-01-01T00:00:00Z");
        var now = DateTimeOffset.Parse("2030-01-01T00:01:03Z");

        var next = _calculator.GetNextExecution(
            """{"type":"interval","unit":"seconds","value":5}""",
            previous,
            now);

        Assert.Equal(DateTimeOffset.Parse("2030-01-01T00:01:05Z"), next);
    }

    [Fact]
    public void Calendar_schedule_preserves_day_and_month_interval()
    {
        var previous = DateTimeOffset.Parse("2030-01-15T00:00:00Z");
        var now = DateTimeOffset.Parse("2030-05-20T12:00:00Z");

        var next = _calculator.GetNextExecution(
            """
            {
              "type":"calendar",
              "unit":"months",
              "interval":3,
              "dayOfMonth":15,
              "timeUtc":"00:00:00"
            }
            """,
            previous,
            now);

        Assert.Equal(DateTimeOffset.Parse("2030-07-15T00:00:00Z"), next);
    }

    [Fact]
    public void Null_schedule_is_one_time()
    {
        Assert.Null(_calculator.GetNextExecution(
            null,
            DateTimeOffset.Parse("2030-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2030-01-01T00:00:01Z")));
    }
}
