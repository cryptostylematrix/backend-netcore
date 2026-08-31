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
        builder.Property(task => task.TaskSourceAddr)
            .HasColumnName("task_source_addr")
            .HasMaxLength(600);
        builder.Property(task => task.PlaceId).HasColumnName("place_id").IsRequired();
        builder.HasOne(task => task.Place)
            .WithMany()
            .HasForeignKey(task => task.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(task => task.ResponseSourcePlaceId)
            .HasColumnName("response_source_place_id")
            .IsRequired();
        builder.HasOne(task => task.ResponseSourcePlace)
            .WithMany()
            .HasForeignKey(task => task.ResponseSourcePlaceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(task => task.ResponseCode)
            .HasColumnName("response_code")
            .HasConversion<long>()
            .IsRequired();
        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at");
        builder.Ignore(task => task.DomainEvents);
    }
}
