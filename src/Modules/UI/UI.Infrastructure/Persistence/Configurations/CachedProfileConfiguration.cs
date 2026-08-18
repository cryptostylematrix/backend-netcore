using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Core.ProfileAggregate;

namespace UI.Infrastructure.Persistence.Configurations;

internal sealed class CachedProfileConfiguration
    : IEntityTypeConfiguration<CachedProfile>
{
    public void Configure(EntityTypeBuilder<CachedProfile> builder)
    {
        builder.ToTable("profiles");
        builder.HasKey(profile => profile.Address);
        builder.Property(profile => profile.Address)
            .HasColumnName("address")
            .HasMaxLength(600);
        builder.Property(profile => profile.Login)
            .HasColumnName("login")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(profile => profile.Login).IsUnique();
        builder.Property(profile => profile.ContentJson)
            .HasColumnName("content")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(profile => profile.UpdatedAtUtc)
            .HasColumnName("updated_at");
        builder.Ignore(profile => profile.DomainEvents);
    }
}
