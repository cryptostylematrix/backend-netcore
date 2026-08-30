using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScheduledTasks.Core.TaskAggregate;

namespace ScheduledTasks.Infrastructure.Persistence;

internal sealed class ScheduledTaskConfiguration
    : IEntityTypeConfiguration<ScheduledTask>
{
    public void Configure(EntityTypeBuilder<ScheduledTask> builder)
    {
        builder.ToTable("tasks");
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id).HasColumnName("id");
        builder.Property(task => task.ExecutionNumber)
            .HasColumnName("execution_number");
        builder.Property(task => task.ExecuteAtUtc)
            .HasColumnName("execute_at_utc");
        builder.Property(task => task.Schedule)
            .HasColumnName("schedule")
            .HasColumnType("jsonb");
        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToLowerInvariant(),
                value => Enum.Parse<ScheduledTaskStatus>(value, ignoreCase: true));
        builder.Property(task => task.Commands)
            .HasColumnName("commands")
            .HasColumnType("jsonb");
        builder.Property(task => task.Error).HasColumnName("error");
        builder.Property(task => task.CreatedAtUtc)
            .HasColumnName("created_at_utc");
        builder.Property(task => task.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
        builder.Property(task => task.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.Ignore(task => task.DomainEvents);
    }
}
