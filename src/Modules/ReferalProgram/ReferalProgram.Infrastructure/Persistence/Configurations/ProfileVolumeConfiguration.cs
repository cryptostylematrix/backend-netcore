using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Infrastructure.Persistence.Configurations;

internal sealed class ProfileVolumeConfiguration : IEntityTypeConfiguration<ProfileVolume>
{
    public void Configure(EntityTypeBuilder<ProfileVolume> builder)
    {
        builder.ToTable("profile_volumes");
        builder.HasKey(volume => new
        {
            volume.MarketingAddr,
            volume.StructureNumber,
            volume.ProfileAddr
        });

        builder.Property(volume => volume.MarketingAddr)
            .HasColumnName("marketing_addr")
            .HasMaxLength(600);
        builder.Property(volume => volume.StructureNumber)
            .HasColumnName("structure_number")
            .HasConversion<short>();
        builder.Property(volume => volume.ProfileAddr)
            .HasColumnName("profile_addr")
            .HasMaxLength(600);
        builder.Property(volume => volume.PersonalVolume)
            .HasColumnName("personal_volume")
            .HasConversion<long>();
        builder.Property(volume => volume.ReferralVolume)
            .HasColumnName("referral_volume")
            .HasConversion<long>();
        builder.Property(volume => volume.GroupVolume)
            .HasColumnName("group_volume")
            .HasConversion<long>();
        builder.Ignore(volume => volume.DomainEvents);
    }
}
