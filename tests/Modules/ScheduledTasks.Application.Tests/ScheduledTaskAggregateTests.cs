using ScheduledTasks.Core.TaskAggregate;

namespace ScheduledTasks.Application.Tests;

public sealed class ScheduledTaskAggregateTests
{
    private static readonly DateTimeOffset ScheduledAt =
        DateTimeOffset.Parse("2030-01-01T00:00:00Z");

    [Fact]
    public void Recurring_completion_advances_occurrence()
    {
        var task = CreateTask("""{"type":"interval","unit":"days","value":1}""");

        task.Complete(ScheduledAt.AddDays(1), ScheduledAt.AddMinutes(1));

        Assert.Equal(2, task.ExecutionNumber);
        Assert.Equal(ScheduledAt.AddDays(1), task.ExecuteAtUtc);
        Assert.Equal(ScheduledTaskStatus.Active, task.Status);
        Assert.Null(task.Error);
    }

    [Fact]
    public void One_time_completion_clears_execution_time()
    {
        var task = CreateTask(null);

        task.Complete(null, ScheduledAt.AddMinutes(1));

        Assert.Equal(ScheduledTaskStatus.Completed, task.Status);
        Assert.Null(task.ExecuteAtUtc);
        Assert.Equal(1, task.ExecutionNumber);
    }

    [Fact]
    public void Failure_and_retry_keep_the_same_occurrence()
    {
        var task = CreateTask(null);
        task.MarkFailed("failure", ScheduledAt.AddMinutes(1));

        task.Retry(ScheduledAt.AddMinutes(2));

        Assert.Equal(ScheduledTaskStatus.Active, task.Status);
        Assert.Equal(ScheduledAt, task.ExecuteAtUtc);
        Assert.Equal(1, task.ExecutionNumber);
        Assert.Null(task.Error);
    }

    [Fact]
    public void Stop_clears_execution_without_rewriting_status()
    {
        var task = CreateTask(null);

        task.Stop(ScheduledAt.AddMinutes(1));

        Assert.Null(task.ExecuteAtUtc);
        Assert.Equal(ScheduledTaskStatus.Active, task.Status);
    }

    private static ScheduledTask CreateTask(string? schedule) =>
        ScheduledTask.Create(
            Guid.NewGuid(),
            ScheduledAt,
            schedule,
            """[{"module":"test","type":"test.command"}]""",
            ScheduledAt.AddDays(-1));
}
