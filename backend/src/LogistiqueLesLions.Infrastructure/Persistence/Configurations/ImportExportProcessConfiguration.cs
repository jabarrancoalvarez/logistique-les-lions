using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class ImportExportProcessConfiguration : IEntityTypeConfiguration<ImportExportProcess>
{
    public void Configure(EntityTypeBuilder<ImportExportProcess> builder)
    {
        builder.ToTable("import_export_processes", "compliance");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.OriginCountry).HasMaxLength(2).IsRequired();
        builder.Property(p => p.DestinationCountry).HasMaxLength(2).IsRequired();
        builder.Property(p => p.ProcessType).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.EstimatedCostEur).HasPrecision(12, 2);
        builder.Property(p => p.ActualCostEur).HasPrecision(12, 2);
        builder.Property(p => p.CancellationReason).HasMaxLength(500);
        builder.Property(p => p.Notes).HasColumnType("jsonb");

        builder.HasIndex(p => p.VehicleId);
        builder.HasIndex(p => p.BuyerId);
        builder.HasIndex(p => p.SellerId);
        builder.HasIndex(p => p.Status);
        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasOne(p => p.Vehicle)
            .WithMany()
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Documents)
            .WithOne(d => d.Process)
            .HasForeignKey(d => d.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
