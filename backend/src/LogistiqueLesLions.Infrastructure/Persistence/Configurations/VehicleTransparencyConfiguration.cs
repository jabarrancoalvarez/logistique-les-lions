using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleTransparencyConfiguration : IEntityTypeConfiguration<VehicleTransparency>
{
    public void Configure(EntityTypeBuilder<VehicleTransparency> builder)
    {
        builder.ToTable("vehicle_transparency", "garage");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        // Un anuncio enseña un solo historial.
        builder.HasIndex(t => t.VehicleId).IsUnique();
        builder.HasIndex(t => t.GarageVehicleId);

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.HasOne(t => t.Vehicle)
            .WithMany()
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.GarageVehicle)
            .WithMany()
            .HasForeignKey(t => t.GarageVehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.SharedRecords)
            .WithOne(r => r.Transparency)
            .HasForeignKey(r => r.TransparencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SharedMaintenanceRecordConfiguration
    : IEntityTypeConfiguration<SharedMaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<SharedMaintenanceRecord> builder)
    {
        builder.ToTable("shared_maintenance_records", "garage");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // Una intervención no puede compartirse dos veces en el mismo anuncio.
        builder.HasIndex(r => new { r.TransparencyId, r.MaintenanceRecordId }).IsUnique();

        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasOne(r => r.MaintenanceRecord)
            .WithMany()
            .HasForeignKey(r => r.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
