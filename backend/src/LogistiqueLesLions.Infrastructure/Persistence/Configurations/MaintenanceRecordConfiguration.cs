using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("maintenance_records", "garage");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(25);
        builder.Property(r => r.Description).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Workshop).HasMaxLength(150);
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.Cost).HasPrecision(12, 2);

        // El historial se lee por vehículo y en orden cronológico.
        builder.HasIndex(r => new { r.GarageVehicleId, r.PerformedAt });

        builder.Ignore(r => r.HasInvoice);

        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasOne(r => r.GarageVehicle)
            .WithMany(v => v.MaintenanceRecords)
            .HasForeignKey(r => r.GarageVehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Si se borra el documento enlazado, la intervención sobrevive sin factura.
        builder.HasOne(r => r.Document)
            .WithMany()
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Images)
            .WithOne(i => i.MaintenanceRecord)
            .HasForeignKey(i => i.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaintenanceRecordImageConfiguration : IEntityTypeConfiguration<MaintenanceRecordImage>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecordImage> builder)
    {
        builder.ToTable("maintenance_record_images", "garage");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(i => i.FileName).HasMaxLength(255).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(100).IsRequired();

        builder.HasIndex(i => i.MaintenanceRecordId);

        builder.HasQueryFilter(i => i.DeletedAt == null);
    }
}
