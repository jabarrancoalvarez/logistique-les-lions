using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleInspectionConfiguration : IEntityTypeConfiguration<VehicleInspection>
{
    public void Configure(EntityTypeBuilder<VehicleInspection> builder)
    {
        builder.ToTable("vehicle_inspections", "messaging");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Notes).HasMaxLength(4000);

        // Una checklist por negociación y usuario: cada parte tiene la suya.
        builder.HasIndex(i => new { i.NegotiationId, i.UserId }).IsUnique();

        builder.HasQueryFilter(i => i.DeletedAt == null);

        builder.HasOne(i => i.Negotiation)
            .WithMany()
            .HasForeignKey(i => i.NegotiationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Items)
            .WithOne(x => x.Inspection)
            .HasForeignKey(x => x.InspectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleInspectionItemConfiguration : IEntityTypeConfiguration<VehicleInspectionItem>
{
    public void Configure(EntityTypeBuilder<VehicleInspectionItem> builder)
    {
        builder.ToTable("vehicle_inspection_items", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        // Un punto de la checklist no puede repetirse dentro de la misma inspección.
        builder.HasIndex(x => new { x.InspectionId, x.Type }).IsUnique();

        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
