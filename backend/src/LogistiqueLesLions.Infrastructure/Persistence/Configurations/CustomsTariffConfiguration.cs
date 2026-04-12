using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class CustomsTariffConfiguration : IEntityTypeConfiguration<CustomsTariff>
{
    public void Configure(EntityTypeBuilder<CustomsTariff> builder)
    {
        builder.ToTable("customs_tariffs", "compliance");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.OriginCountry).HasMaxLength(2).IsRequired();
        builder.Property(t => t.DestinationCountry).HasMaxLength(2).IsRequired();
        builder.Property(t => t.HsCode).HasMaxLength(10).IsRequired();
        builder.Property(t => t.TariffRatePercent).HasPrecision(5, 2);
        builder.Property(t => t.Source).HasMaxLength(500);

        builder.HasIndex(t => new { t.OriginCountry, t.DestinationCountry, t.HsCode });
        builder.HasIndex(t => t.ValidFrom);
        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
