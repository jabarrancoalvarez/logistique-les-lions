using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class PriceIndicatorSettingsConfiguration : IEntityTypeConfiguration<PriceIndicatorSettings>
{
    public void Configure(EntityTypeBuilder<PriceIndicatorSettings> builder)
    {
        builder.ToTable("price_indicator_settings", "vehicles");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.GoodDealMargin).HasPrecision(4, 3);
        builder.Property(s => s.HighPriceMargin).HasPrecision(4, 3);

        builder.HasQueryFilter(s => s.DeletedAt == null);

        // Timestamp fijo: con DateTimeOffset.UtcNow cada `migrations add` generaría un
        // UpdateData espurio sobre esta fila.
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(new PriceIndicatorSettings
        {
            Id                = PriceIndicatorSettings.SingletonId,
            MinComparables    = 5,
            MaxListingAgeDays = 180,
            YearBand          = 2,
            GoodDealMargin    = 0.10m,
            HighPriceMargin   = 0.10m,
            CreatedAt         = seededAt,
            UpdatedAt         = seededAt
        });
    }
}
