using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehiclePriceHistoryConfiguration : IEntityTypeConfiguration<VehiclePriceHistory>
{
    public void Configure(EntityTypeBuilder<VehiclePriceHistory> builder)
    {
        builder.ToTable("vehicle_price_history", "vehicles");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.Price).HasPrecision(12, 2);

        // Se consulta siempre por anuncio y en orden cronológico.
        builder.HasIndex(h => new { h.VehicleId, h.ChangedAt });

        builder.HasOne(h => h.Vehicle)
            .WithMany(v => v.PriceHistory)
            .HasForeignKey(h => h.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
