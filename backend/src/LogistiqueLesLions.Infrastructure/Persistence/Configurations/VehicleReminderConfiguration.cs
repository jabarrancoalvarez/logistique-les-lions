using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleReminderConfiguration : IEntityTypeConfiguration<VehicleReminder>
{
    public void Configure(EntityTypeBuilder<VehicleReminder> builder)
    {
        builder.ToTable("vehicle_reminders", "garage");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Label).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasIndex(r => new { r.GarageVehicleId, r.Status });
        // El trabajo en segundo plano busca los que vencen por fecha y siguen abiertos.
        builder.HasIndex(r => new { r.Status, r.DueDate });

        builder.Ignore(r => r.IsOpen);

        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasOne(r => r.GarageVehicle)
            .WithMany(v => v.Reminders)
            .HasForeignKey(r => r.GarageVehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
