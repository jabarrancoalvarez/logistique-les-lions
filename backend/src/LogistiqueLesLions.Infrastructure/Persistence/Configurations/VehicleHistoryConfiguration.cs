using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleHistoryConfiguration : IEntityTypeConfiguration<VehicleHistory>
{
    public void Configure(EntityTypeBuilder<VehicleHistory> builder)
    {
        builder.ToTable("vehicle_histories", "vehicles");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.Description).HasMaxLength(1000).IsRequired();
        builder.Property(h => h.Source).HasMaxLength(200);
        builder.Property(h => h.EventType).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(h => h.VehicleId);
        builder.HasIndex(h => h.EventDate);
        builder.HasQueryFilter(h => h.DeletedAt == null);

        builder.HasOne(h => h.Vehicle)
            .WithMany()
            .HasForeignKey(h => h.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
