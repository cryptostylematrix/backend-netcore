using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReferalProgram.Core.MarketingTaskAggregate;

namespace ReferalProgram.Infrastructure.Persistence.Configurations;

internal sealed class MarketingTaskConfiguration
    : IEntityTypeConfiguration<MarketingTask>
{
    public void Configure(EntityTypeBuilder<MarketingTask> builder)
    {
        builder.ToTable("marketing_tasks");
        builder.HasKey(task => new { task.MarketingAddr, task.TaskKey });

        builder.Property(task => task.MarketingAddr)
            .HasColumnName("marketing_addr")
            .IsRequired();
        builder.Property(task => task.TaskKey)
            .HasColumnName("task_key");
        builder.Property(task => task.TaskQueryId)
            .HasColumnName("task_query_id");
        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToLowerInvariant(),
                value => Enum.Parse<MarketingTaskStatus>(value, ignoreCase: true))
            .IsRequired();
        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at");
        builder.Property(task => task.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Ignore(task => task.DomainEvents);
    }
}
