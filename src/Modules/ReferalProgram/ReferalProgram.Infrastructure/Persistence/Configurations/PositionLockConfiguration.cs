using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReferalProgram.Core.LockAggregate;

namespace ReferalProgram.Infrastructure.Persistence.Configurations;

internal sealed class PositionLockConfiguration : IEntityTypeConfiguration<PositionLock>
{
    public void Configure(EntityTypeBuilder<PositionLock> builder)
    {
        builder.ToTable("locks");
        builder.HasKey(positionLock => positionLock.Id);

        builder.Property(positionLock => positionLock.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        builder.Property(positionLock => positionLock.TaskKey).HasColumnName("task_key");
        builder.Property(positionLock => positionLock.TaskQueryId).HasColumnName("task_query_id");
        builder.Property(positionLock => positionLock.TaskSourceAddr)
            .HasColumnName("task_source_addr")
            .HasMaxLength(600);
        builder.Property(positionLock => positionLock.MarketingAddr)
            .HasColumnName("marketing_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(positionLock => positionLock.StructureNumber)
            .HasColumnName("structure_number")
            .HasConversion<short>();
        builder.Property(positionLock => positionLock.PlaceProfileAddr)
            .HasColumnName("place_profile_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(positionLock => positionLock.PlaceNumber)
            .HasColumnName("place_number")
            .HasConversion<long>();
        builder.Property(positionLock => positionLock.PlaceProfileLogin)
            .HasColumnName("place_profile_login")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(positionLock => positionLock.ProfileAddr)
            .HasColumnName("profile_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(positionLock => positionLock.LockedPos)
            .HasColumnName("locked_pos")
            .HasConversion<long>();
        builder.Property(positionLock => positionLock.Mp)
            .HasColumnName("mp")
            .IsRequired();
        builder.Property(positionLock => positionLock.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(positionLock => new
        {
            positionLock.MarketingAddr,
            positionLock.StructureNumber,
            positionLock.PlaceProfileAddr,
            positionLock.PlaceNumber,
            positionLock.ProfileAddr,
            positionLock.LockedPos
        }).IsUnique();

        builder.Ignore(positionLock => positionLock.DomainEvents);
    }
}
