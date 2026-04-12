using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class SavedVehicleConfiguration : IEntityTypeConfiguration<SavedVehicle>
{
    public void Configure(EntityTypeBuilder<SavedVehicle> builder)
    {
        builder.ToTable("saved_vehicles", "vehicles");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // Un usuario no puede guardar el mismo vehículo dos veces
        builder.HasIndex(s => new { s.UserId, s.VehicleId }).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasQueryFilter(s => s.DeletedAt == null);

        builder.HasOne(s => s.Vehicle)
            .WithMany()
            .HasForeignKey(s => s.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
