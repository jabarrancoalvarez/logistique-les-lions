using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> builder)
    {
        builder.ToTable("vehicle_models", "vehicles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Category).HasMaxLength(50);
        builder.HasIndex(m => new { m.MakeId, m.Name }).IsUnique();
        builder.HasQueryFilter(m => m.DeletedAt == null);

        // Almacenar lista de BodyType como array de texto en PostgreSQL
        builder.Property(m => m.BodyTypes)
            .HasConversion(
                v => string.Join(',', v.Select(bt => bt.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Enum.Parse<BodyType>(s)).ToList()
            );
    }
}
