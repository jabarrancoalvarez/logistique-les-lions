using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class ProcessDocumentConfiguration : IEntityTypeConfiguration<ProcessDocument>
{
    public void Configure(EntityTypeBuilder<ProcessDocument> builder)
    {
        builder.ToTable("process_documents", "compliance");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Country).HasMaxLength(2).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.ResponsibleParty).HasMaxLength(20).IsRequired();
        builder.Property(d => d.FileUrl).HasMaxLength(1000);
        builder.Property(d => d.TemplateUrl).HasMaxLength(1000);
        builder.Property(d => d.OfficialUrl).HasMaxLength(1000);
        builder.Property(d => d.InstructionsEs).HasMaxLength(3000);
        builder.Property(d => d.EstimatedCostEur).HasPrecision(10, 2);

        builder.HasIndex(d => d.ProcessId);
        builder.HasIndex(d => d.Status);
        builder.HasQueryFilter(d => d.DeletedAt == null);
    }
}
