using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class HomologationRequirementConfiguration : IEntityTypeConfiguration<HomologationRequirement>
{
    public void Configure(EntityTypeBuilder<HomologationRequirement> builder)
    {
        builder.ToTable("homologation_requirements", "compliance");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.DestinationCountry).HasMaxLength(2).IsRequired();
        builder.Property(h => h.VehicleCategory).HasMaxLength(10).IsRequired();
        builder.Property(h => h.EmissionStandard).HasMaxLength(20);
        builder.Property(h => h.RequiredModifications).HasColumnType("jsonb");
        builder.Property(h => h.EstimatedCostEur).HasPrecision(10, 2);
        builder.Property(h => h.CertifyingBody).HasMaxLength(200);

        builder.HasIndex(h => h.DestinationCountry);
        builder.HasIndex(h => new { h.DestinationCountry, h.VehicleCategory });
        builder.HasQueryFilter(h => h.DeletedAt == null);
    }
}
