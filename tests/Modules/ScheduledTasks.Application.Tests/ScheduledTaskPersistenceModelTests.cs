using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ScheduledTasks.Core.TaskAggregate;
using ScheduledTasks.Infrastructure.Persistence;

namespace ScheduledTasks.Application.Tests;

public sealed class ScheduledTaskPersistenceModelTests
{
    [Fact]
    public void Maps_the_aggregate_to_the_single_tasks_table()
    {
        var options = new DbContextOptionsBuilder<ScheduledTasksDataContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test")
            .Options;
        using var context = new ScheduledTasksDataContext(options);

        var entity = context.Model.FindEntityType(typeof(ScheduledTask));

        Assert.NotNull(entity);
        Assert.Equal("tasks", entity.GetTableName());
        Assert.Equal("jsonb", entity.FindProperty(nameof(ScheduledTask.Commands))!.GetColumnType());
        Assert.Equal("jsonb", entity.FindProperty(nameof(ScheduledTask.Schedule))!.GetColumnType());
        var version = entity.FindProperty(nameof(ScheduledTask.Version))!;
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
    }
}
