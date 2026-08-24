using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Infrastructure.Persistence.Configurations;

internal sealed class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("places");
        builder.HasKey(place => place.Id);

        builder.Property(place => place.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        builder.Property(place => place.ParentId).HasColumnName("parent_id");
        builder.Property(place => place.Mp).HasColumnName("mp").IsRequired();
        builder.Property(place => place.PosGroup).HasColumnName("pos_group").HasConversion<short>();
        builder.Property(place => place.MarketingAddr).HasColumnName("marketing_addr").HasMaxLength(600).IsRequired();
        builder.Property(place => place.StructureNumber).HasColumnName("structure_number").HasConversion<short>();
        builder.Property(place => place.ProfileAddr).HasColumnName("profile_addr").HasMaxLength(600);
        builder.Property(place => place.PlaceNumber).HasColumnName("place_number").HasConversion<long>();
        builder.Property(place => place.ProfileLogin).HasColumnName("profile_login").HasMaxLength(50);
        builder.Property(place => place.Index).HasColumnName("index").IsRequired();
        builder.Property(place => place.ParentProfileAddr).HasColumnName("parent_profile_addr").HasMaxLength(600);
        builder.Property(place => place.ParentProfileLogin).HasColumnName("parent_profile_login").HasMaxLength(50);
        builder.Property(place => place.ParentPlaceNumber).HasColumnName("parent_place_number").HasConversion<long?>();
        builder.Property(place => place.CreatedAt).HasColumnName("created_at");
        builder.Property(place => place.ActivatedAt).HasColumnName("activated_at");
        builder.Property(place => place.IsActive).HasColumnName("is_active");
        builder.Property(place => place.Kind).HasColumnName("kind").HasConversion<short>();
        builder.Property(place => place.Pos).HasColumnName("pos").HasConversion<long>();
        builder.Property(place => place.Filling)
            .HasColumnName("filling")
            .HasConversion<long>()
            .IsConcurrencyToken();
        builder.Property(place => place.Deep).HasColumnName("deep").HasConversion<long>();
        builder.Property(place => place.PersonalVolume).HasColumnName("personal_volume").HasConversion<long>();
        builder.Property(place => place.GroupVolume).HasColumnName("group_volume").HasConversion<long>();
        builder.Property(place => place.MatrixFilling).HasColumnName("matrix_filling");
        builder.Property(place => place.TaskKey).HasColumnName("task_key");
        builder.Property(place => place.TaskQueryId).HasColumnName("task_query_id");
        builder.Property(place => place.TaskSourceAddr).HasColumnName("task_source_addr").HasMaxLength(600);

        builder.Ignore(place => place.DomainEvents);
    }
}
