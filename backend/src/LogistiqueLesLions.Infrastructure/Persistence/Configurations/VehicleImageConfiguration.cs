using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImage>
{
    public void Configure(EntityTypeBuilder<VehicleImage> builder)
    {
        builder.ToTable("vehicle_images", "vehicles");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Url).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.ThumbnailUrl).HasMaxLength(1000);
        builder.Property(i => i.Format).HasMaxLength(10).HasDefaultValue("webp");
        builder.HasIndex(i => new { i.VehicleId, i.IsPrimary });
        builder.HasIndex(i => new { i.VehicleId, i.SortOrder });
        builder.HasQueryFilter(i => i.DeletedAt == null);
    }
}
