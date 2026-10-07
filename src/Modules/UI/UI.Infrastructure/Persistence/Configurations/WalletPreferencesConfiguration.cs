using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Core.WalletPreferencesAggregate;

namespace UI.Infrastructure.Persistence.Configurations;

internal sealed class WalletPreferencesConfiguration : IEntityTypeConfiguration<WalletPreferences>
{
    public void Configure(EntityTypeBuilder<WalletPreferences> builder)
    {
        builder.ToTable("wallet_preferences");
        builder.HasKey(x => x.WalletAddr);
        builder.Property(x => x.WalletAddr).HasColumnName("wallet_addr").HasMaxLength(600);
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(63).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Ignore(x => x.DomainEvents);
    }
}
