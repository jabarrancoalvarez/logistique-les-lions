using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleValuationSettingsConfiguration
    : IEntityTypeConfiguration<VehicleValuationSettings>
{
    public void Configure(EntityTypeBuilder<VehicleValuationSettings> builder)
    {
        builder.ToTable("valuation_settings", "vehicles");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.RangeSpread).HasPrecision(4, 3);

        builder.HasQueryFilter(s => s.DeletedAt == null);

        // Timestamp fijo: con DateTimeOffset.UtcNow cada `migrations add` generaría un
        // UpdateData espurio sobre esta fila.
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(new VehicleValuationSettings
        {
            Id                   = VehicleValuationSettings.SingletonId,
            MinComparables       = 5,
            MaxListingAgeDays    = 365,
            YearBand             = 2,
            MileageBandKm        = 30_000,
            RangeSpread          = 0.05m,
            SnapshotIntervalDays = 30,
            CreatedAt            = seededAt,
            UpdatedAt            = seededAt
        });
    }
}

public class VehicleValuationSnapshotConfiguration
    : IEntityTypeConfiguration<VehicleValuationSnapshot>
{
    public void Configure(EntityTypeBuilder<VehicleValuationSnapshot> builder)
    {
        builder.ToTable("valuation_snapshots", "garage");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.EstimatedValue).HasPrecision(12, 2);
        builder.Property(s => s.LowValue).HasPrecision(12, 2);
        builder.Property(s => s.HighValue).HasPrecision(12, 2);

        builder.HasIndex(s => new { s.GarageVehicleId, s.CapturedAt });

        builder.HasQueryFilter(s => s.DeletedAt == null);

        builder.HasOne(s => s.GarageVehicle)
            .WithMany(v => v.ValuationSnapshots)
            .HasForeignKey(s => s.GarageVehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
