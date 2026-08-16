using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class GarageVehicleConfiguration : IEntityTypeConfiguration<GarageVehicle>
{
    public void Configure(EntityTypeBuilder<GarageVehicle> builder)
    {
        builder.ToTable("garage_vehicles", "garage");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Version).HasMaxLength(100);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.RegistrationPlate).HasMaxLength(20);
        builder.Property(v => v.Vin).HasMaxLength(17);
        builder.Property(v => v.PurchasePrice).HasPrecision(12, 2);

        builder.Property(v => v.FuelType).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Transmission).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.BodyType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(v => v.UserId);
        // Una venta verificada no puede generar dos veces el mismo vehículo en el garaje.
        // El índice es parcial: si el usuario lo quita del garaje, debe poder volver a
        // añadirlo desde la misma compra.
        builder.HasIndex(v => v.SourceContractId)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Ignore(v => v.BoughtOnYoonUAuto);

        builder.HasQueryFilter(v => v.DeletedAt == null);

        builder.HasOne(v => v.Make)
            .WithMany()
            .HasForeignKey(v => v.MakeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Model)
            .WithMany()
            .HasForeignKey(v => v.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Images)
            .WithOne(i => i.GarageVehicle)
            .HasForeignKey(i => i.GarageVehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GarageDocumentConfiguration : IEntityTypeConfiguration<GarageDocument>
{
    public void Configure(EntityTypeBuilder<GarageDocument> builder)
    {
        builder.ToTable("garage_documents", "garage");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(25);
        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(1000);

        // El historial se lee por vehículo y en orden cronológico.
        builder.HasIndex(d => new { d.GarageVehicleId, d.DocumentDate });

        builder.HasQueryFilter(d => d.DeletedAt == null);

        builder.HasOne(d => d.GarageVehicle)
            .WithMany(v => v.Documents)
            .HasForeignKey(d => d.GarageVehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GarageVehicleImageConfiguration : IEntityTypeConfiguration<GarageVehicleImage>
{
    public void Configure(EntityTypeBuilder<GarageVehicleImage> builder)
    {
        builder.ToTable("garage_vehicle_images", "garage");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(i => i.FileName).HasMaxLength(255).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(100).IsRequired();

        builder.HasIndex(i => i.GarageVehicleId);

        builder.HasQueryFilter(i => i.DeletedAt == null);
    }
}
