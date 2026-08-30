using ScheduledTasks.Application;

namespace ScheduledTasks.Application.Tests;

public sealed class DeterministicCorrelationIdTests
{
    [Fact]
    public void Same_occurrence_and_sequence_produce_same_id()
    {
        var taskId = Guid.Parse("fa942b1d-696b-41f5-8db0-5d4a16b8b0ee");

        var first = DeterministicCorrelationId.Create(taskId, 7, 2);
        var retry = DeterministicCorrelationId.Create(taskId, 7, 2);

        Assert.Equal(first, retry);
        Assert.Equal(5, first.Version);
    }

    [Fact]
    public void New_occurrence_and_command_position_produce_new_ids()
    {
        var taskId = Guid.Parse("fa942b1d-696b-41f5-8db0-5d4a16b8b0ee");
        var original = DeterministicCorrelationId.Create(taskId, 7, 2);

        Assert.NotEqual(original, DeterministicCorrelationId.Create(taskId, 8, 2));
        Assert.NotEqual(original, DeterministicCorrelationId.Create(taskId, 7, 3));
    }
}
