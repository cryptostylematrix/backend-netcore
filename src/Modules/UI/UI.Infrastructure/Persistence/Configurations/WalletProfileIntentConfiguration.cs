using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Core.ProfileAggregate;
using UI.Core.WalletProfileIntentAggregate;

namespace UI.Infrastructure.Persistence.Configurations;

internal sealed class WalletProfileIntentConfiguration
    : IEntityTypeConfiguration<WalletProfileIntent>
{
    public void Configure(EntityTypeBuilder<WalletProfileIntent> builder)
    {
        builder.ToTable("wallet_profile_intents");
        builder.HasKey(intent => intent.Id);
        builder.Property(intent => intent.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        builder.Property(intent => intent.WalletAddr)
            .HasColumnName("wallet_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(intent => intent.ProfileAddr)
            .HasColumnName("profile_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(intent => intent.Mode)
            .HasColumnName("mode")
            .HasConversion(
                mode => mode.ToString().ToLowerInvariant(),
                value => Enum.Parse<WalletProfileMode>(value, ignoreCase: true));
        builder.Property(intent => intent.Owned).HasColumnName("owned");
        builder.Property(intent => intent.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(intent => intent.UpdatedAtUtc).HasColumnName("updated_at");
        builder.HasIndex(intent => new { intent.WalletAddr, intent.ProfileAddr })
            .IsUnique();
        builder.HasOne<CachedProfile>()
            .WithMany()
            .HasForeignKey(intent => intent.ProfileAddr)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(intent => intent.DomainEvents);
    }
}
