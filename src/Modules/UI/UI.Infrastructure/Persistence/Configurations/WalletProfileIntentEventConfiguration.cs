using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Core.WalletProfileIntentAggregate;

namespace UI.Infrastructure.Persistence.Configurations;

internal sealed class WalletProfileIntentEventConfiguration
    : IEntityTypeConfiguration<WalletProfileIntentEvent>
{
    public void Configure(EntityTypeBuilder<WalletProfileIntentEvent> builder)
    {
        builder.ToTable("wallet_profile_intent_events");
        builder.HasKey(eventItem => eventItem.Id);
        builder.Property(eventItem => eventItem.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        builder.Property(eventItem => eventItem.EventId)
            .HasColumnName("event_id");
        builder.HasIndex(eventItem => eventItem.EventId).IsUnique();
        builder.Property(eventItem => eventItem.WalletAddr)
            .HasColumnName("wallet_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(eventItem => eventItem.ProfileAddr)
            .HasColumnName("profile_addr")
            .HasMaxLength(600)
            .IsRequired();
        builder.Property(eventItem => eventItem.EventType)
            .HasColumnName("event_type")
            .HasConversion(
                eventType => ToDatabaseValue(eventType),
                value => FromDatabaseValue(value));
        builder.Property(eventItem => eventItem.DataJson)
            .HasColumnName("data")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(eventItem => eventItem.OccurredAtUtc)
            .HasColumnName("occurred_at");
    }

    private static string ToDatabaseValue(WalletProfileIntentEventType eventType) =>
        eventType switch
        {
            WalletProfileIntentEventType.Added => "added",
            WalletProfileIntentEventType.Removed => "removed",
            WalletProfileIntentEventType.OwnershipLost => "ownership_lost",
            WalletProfileIntentEventType.OwnershipGained => "ownership_gained",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType))
        };

    private static WalletProfileIntentEventType FromDatabaseValue(string value) =>
        value switch
        {
            "added" => WalletProfileIntentEventType.Added,
            "removed" => WalletProfileIntentEventType.Removed,
            "ownership_lost" => WalletProfileIntentEventType.OwnershipLost,
            "ownership_gained" => WalletProfileIntentEventType.OwnershipGained,
            _ => throw new InvalidOperationException(
                $"Unknown wallet profile event type '{value}'.")
        };
}
