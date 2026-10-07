using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Core.TonConnectionAggregate;

namespace UI.Infrastructure.Persistence.Configurations;

internal sealed class TonConnectionConfiguration : IEntityTypeConfiguration<TonConnection>
{
    public void Configure(EntityTypeBuilder<TonConnection> builder)
    {
        builder.ToTable("ton_connections");
        builder.HasKey(x => x.WalletAddr);
        builder.Property(x => x.WalletAddr).HasColumnName("wallet_addr").HasMaxLength(600);
        builder.Property(x => x.ContractVersion).HasColumnName("contract_version").HasMaxLength(32).IsRequired();
        builder.Property(x => x.WalletName).HasColumnName("wallet_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.AppVersion).HasColumnName("app_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Platform).HasColumnName("platform").HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(x => x.LastConnectedAtUtc).HasColumnName("last_connected_at");
        builder.Ignore(x => x.DomainEvents);
    }
}
