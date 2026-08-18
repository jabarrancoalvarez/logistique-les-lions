using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", "vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        // ─── Índices ────────────────────────────────────────────────────────
        builder.HasIndex(v => v.Slug).IsUnique();
        builder.HasIndex(v => v.PublicReference).IsUnique();
        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.MakeId);
        builder.HasIndex(v => v.CountryOrigin);
        // Filtros del Marketplace con especial protagonismo en Senegal.
        builder.HasIndex(v => new { v.Region, v.City });
        builder.HasIndex(v => new { v.Status, v.FeaturedTier, v.FeaturedUntil })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_vehicles_active_featured");
        builder.HasIndex(v => new { v.Price, v.Currency });
        builder.HasIndex(v => v.CreatedAt);

        // ─── Propiedades ────────────────────────────────────────────────────
        builder.Property(v => v.PublicReference).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Slug).HasMaxLength(300).IsRequired();
        builder.Property(v => v.Title).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Description).HasMaxLength(5000);
        builder.Property(v => v.Version).HasMaxLength(100);
        builder.Property(v => v.EngineName).HasMaxLength(100);
        builder.Property(v => v.Currency).HasMaxLength(3).HasDefaultValue("XOF");
        builder.Property(v => v.CountryOrigin).HasMaxLength(2).IsRequired().HasDefaultValue("SN");
        builder.Property(v => v.Region).HasMaxLength(10);
        builder.Property(v => v.City).HasMaxLength(100);
        builder.Property(v => v.District).HasMaxLength(100);
        builder.Property(v => v.PostalCode).HasMaxLength(20);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.Vin).HasMaxLength(17);
        builder.Property(v => v.Price).HasPrecision(12, 2);

        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.FeaturedTier).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Condition).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.FuelType).HasConversion<string>().HasMaxLength(30);
        builder.Property(v => v.Transmission).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.BodyType).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Drivetrain).HasConversion<string>().HasMaxLength(20);

        // SearchVector is a computed tsvector column added via raw SQL in migrations
        builder.Ignore(v => v.SearchVector);
        builder.Ignore(v => v.IsPubliclyVisible);
        builder.Ignore(v => v.AcceptsNegotiation);
        builder.Ignore(v => v.HasPublicPage);

        // ─── Soft delete filter ──────────────────────────────────────────────
        builder.HasQueryFilter(v => v.DeletedAt == null);

        // ─── Relaciones ──────────────────────────────────────────────────────
        builder.HasOne(v => v.Make)
            .WithMany(m => m.Vehicles)
            .HasForeignKey(v => v.MakeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Model)
            .WithMany(m => m.Vehicles)
            .HasForeignKey(v => v.ModelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.Images)
            .WithOne(i => i.Vehicle)
            .HasForeignKey(i => i.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Seller)
            .WithMany()
            .HasForeignKey(v => v.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
