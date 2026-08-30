using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReferalProgram.Core.ProgramAggregate;
using ReferalProgramAggregate = ReferalProgram.Core.ProgramAggregate.ReferalProgram;

namespace ReferalProgram.Infrastructure.Persistence.Configurations;

internal sealed class ReferalProgramConfiguration
    : IEntityTypeConfiguration<ReferalProgramAggregate>
{
    public void Configure(
        EntityTypeBuilder<ReferalProgramAggregate> builder)
    {
        builder.ToTable("referal_program");
        builder.HasKey(program => program.MarketingAddr);

        builder.Property(program => program.MarketingAddr)
            .HasColumnName("marketing_addr")
            .ValueGeneratedNever();
        builder.Property(program => program.IsTaskProcessingEnabled)
            .HasColumnName("is_task_processing_enabled")
            .IsRequired();

        builder.Ignore(program => program.DomainEvents);
    }
}
