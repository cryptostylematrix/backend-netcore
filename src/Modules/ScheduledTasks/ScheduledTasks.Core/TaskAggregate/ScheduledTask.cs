using Common.Domain;

namespace ScheduledTasks.Core.TaskAggregate;

public sealed class ScheduledTask : Entity, IAggregateRoot
{
    private ScheduledTask()
    {
    }

    private ScheduledTask(
        Guid id,
        DateTimeOffset executeAtUtc,
        string? schedule,
        string commands,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Task ID cannot be empty.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(commands);
        EnsureUtc(executeAtUtc, nameof(executeAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        ExecutionNumber = 1;
        ExecuteAtUtc = executeAtUtc;
        Schedule = schedule;
        Status = ScheduledTaskStatus.Active;
        Commands = commands;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public long ExecutionNumber { get; private set; }
    public DateTimeOffset? ExecuteAtUtc { get; private set; }
    public string? Schedule { get; private set; }
    public ScheduledTaskStatus Status { get; private set; }
    public string Commands { get; private set; } = null!;
    public string? Error { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    public static ScheduledTask Create(
        Guid id,
        DateTimeOffset executeAtUtc,
        string? schedule,
        string commands,
        DateTimeOffset createdAtUtc) =>
        new(id, executeAtUtc, schedule, commands, createdAtUtc);

    public void Complete(DateTimeOffset? nextExecutionAtUtc, DateTimeOffset completedAtUtc)
    {
        EnsureExecutable();
        EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        if (nextExecutionAtUtc is not null)
            EnsureUtc(nextExecutionAtUtc.Value, nameof(nextExecutionAtUtc));

        if (nextExecutionAtUtc is null)
        {
            ExecuteAtUtc = null;
            Status = ScheduledTaskStatus.Completed;
        }
        else
        {
            if (nextExecutionAtUtc <= completedAtUtc)
                throw new ArgumentOutOfRangeException(
                    nameof(nextExecutionAtUtc),
                    "The next execution must be in the future.");

            ExecutionNumber = checked(ExecutionNumber + 1);
            ExecuteAtUtc = nextExecutionAtUtc;
            Status = ScheduledTaskStatus.Active;
        }

        Error = null;
        UpdatedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string error, DateTimeOffset failedAtUtc)
    {
        EnsureExecutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        EnsureUtc(failedAtUtc, nameof(failedAtUtc));

        Status = ScheduledTaskStatus.Error;
        Error = error;
        UpdatedAtUtc = failedAtUtc;
    }

    public void Retry(DateTimeOffset retriedAtUtc)
    {
        EnsureUtc(retriedAtUtc, nameof(retriedAtUtc));
        if (Status != ScheduledTaskStatus.Error || ExecuteAtUtc is null)
            throw new InvalidOperationException("Only a scheduled errored task can be retried.");

        Status = ScheduledTaskStatus.Active;
        Error = null;
        UpdatedAtUtc = retriedAtUtc;
    }

    public void Stop(DateTimeOffset stoppedAtUtc)
    {
        EnsureUtc(stoppedAtUtc, nameof(stoppedAtUtc));
        ExecuteAtUtc = null;
        UpdatedAtUtc = stoppedAtUtc;
    }

    private void EnsureExecutable()
    {
        if (Status != ScheduledTaskStatus.Active || ExecuteAtUtc is null)
            throw new InvalidOperationException("Only a scheduled active task can be executed.");
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Scheduled-task timestamps must use UTC.",
                parameterName);
        }
    }
}
