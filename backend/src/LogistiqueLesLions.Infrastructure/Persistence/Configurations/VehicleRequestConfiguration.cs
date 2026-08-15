using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleRequestConfiguration : IEntityTypeConfiguration<VehicleRequest>
{
    public void Configure(EntityTypeBuilder<VehicleRequest> builder)
    {
        builder.ToTable("vehicle_requests", "vehicles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.PublicReference).HasMaxLength(20).IsRequired();
        builder.HasIndex(r => r.PublicReference).IsUnique();

        builder.Property(r => r.MakeName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ModelName).HasMaxLength(100);
        builder.Property(r => r.Version).HasMaxLength(100);
        builder.Property(r => r.Color).HasMaxLength(50);
        builder.Property(r => r.ImportantEquipment).HasMaxLength(1000);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.MaxBudget).HasPrecision(12, 2);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Origin).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.FuelType).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Transmission).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.BodyType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => r.UserId);
        // El backoffice lista por estado y por fecha.
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        builder.Ignore(r => r.CanBeCancelled);
        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Make)
            .WithMany()
            .HasForeignKey(r => r.MakeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Si el administrador responsable deja de existir, la solicitud sobrevive sin
        // responsable: es trabajo pendiente, no un dato del usuario.
        builder.HasOne(r => r.AssignedAdmin)
            .WithMany()
            .HasForeignKey(r => r.AssignedAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Messages)
            .WithOne(m => m.Request)
            .HasForeignKey(m => m.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Proposals)
            .WithOne(p => p.Request)
            .HasForeignKey(p => p.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleRequestMessageConfiguration : IEntityTypeConfiguration<VehicleRequestMessage>
{
    public void Configure(EntityTypeBuilder<VehicleRequestMessage> builder)
    {
        builder.ToTable("vehicle_request_messages", "vehicles");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Body).HasMaxLength(4000).IsRequired();

        builder.HasIndex(m => new { m.RequestId, m.CreatedAt });
        builder.HasQueryFilter(m => m.DeletedAt == null);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class VehicleRequestProposalConfiguration : IEntityTypeConfiguration<VehicleRequestProposal>
{
    public void Configure(EntityTypeBuilder<VehicleRequestProposal> builder)
    {
        builder.ToTable("vehicle_request_proposals", "vehicles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.MakeModel).HasMaxLength(200);
        builder.Property(p => p.Version).HasMaxLength(100);
        builder.Property(p => p.FuelType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Transmission).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.AdditionalCosts).HasPrecision(12, 2);
        builder.Property(p => p.CountryOfOrigin).HasMaxLength(100);
        builder.Property(p => p.PhotoUrls).HasMaxLength(2000);
        builder.Property(p => p.ExternalUrl).HasMaxLength(1000);
        builder.Property(p => p.Comments).HasMaxLength(2000);
        builder.Property(p => p.EstimatedPrice).HasPrecision(12, 2);

        builder.HasIndex(p => p.RequestId);
        builder.Ignore(p => p.IsInternal);
        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasOne(p => p.Vehicle)
            .WithMany()
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
