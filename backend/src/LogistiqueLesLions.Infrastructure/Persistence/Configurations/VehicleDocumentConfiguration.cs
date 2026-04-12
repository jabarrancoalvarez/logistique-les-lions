using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleDocumentConfiguration : IEntityTypeConfiguration<VehicleDocument>
{
    public void Configure(EntityTypeBuilder<VehicleDocument> builder)
    {
        builder.ToTable("vehicle_documents", "vehicles");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Country).HasMaxLength(2).IsRequired();
        builder.Property(d => d.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(d => new { d.VehicleId, d.Type });
        builder.HasQueryFilter(d => d.DeletedAt == null);

        builder.HasOne(d => d.Vehicle)
            .WithMany()
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
