using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.ToTable("document_templates", "compliance");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Country).HasMaxLength(2).IsRequired();
        builder.Property(t => t.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.TemplateUrl).HasMaxLength(1000);
        builder.Property(t => t.InstructionsEs).HasMaxLength(3000);
        builder.Property(t => t.InstructionsEn).HasMaxLength(3000);
        builder.Property(t => t.OfficialUrl).HasMaxLength(1000);
        builder.Property(t => t.IssuingAuthority).HasMaxLength(200);
        builder.Property(t => t.EstimatedCostEur).HasPrecision(10, 2);

        builder.HasIndex(t => new { t.Country, t.DocumentType });
        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
