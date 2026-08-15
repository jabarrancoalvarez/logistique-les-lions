using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts", "messaging");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.PublicReference).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.PublicReference).IsUnique();

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(25);
        builder.Property(c => c.AgreedPrice).HasPrecision(12, 2);

        builder.Property(c => c.VehicleMake).HasMaxLength(100).IsRequired();
        builder.Property(c => c.VehicleModel).HasMaxLength(100);
        builder.Property(c => c.VehicleVersion).HasMaxLength(100);
        builder.Property(c => c.VehicleVin).HasMaxLength(17);
        builder.Property(c => c.RegistrationPlate).HasMaxLength(20);
        builder.Property(c => c.VehicleReference).HasMaxLength(20).IsRequired();

        builder.Property(c => c.SellerLegalName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.SellerIdDocument).HasMaxLength(50);
        builder.Property(c => c.SellerAddress).HasMaxLength(300);
        builder.Property(c => c.BuyerLegalName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.BuyerIdDocument).HasMaxLength(50);
        builder.Property(c => c.BuyerAddress).HasMaxLength(300);

        builder.Property(c => c.ChangeRequestNotes).HasMaxLength(2000);

        builder.Property(c => c.VerificationCode).HasMaxLength(40);
        builder.HasIndex(c => c.VerificationCode).IsUnique();

        // Un solo contrato vivo por negociación. El índice es parcial a propósito: un
        // contrato anulado no debe impedir redactar otro para la misma operación.
        builder.HasIndex(c => c.NegotiationId)
            .IsUnique()
            .HasFilter("status <> 'Annule'");
        builder.HasIndex(c => new { c.Status, c.CreatedAt });

        builder.Ignore(c => c.IsEditable);
        builder.Ignore(c => c.AwaitsValidation);
        builder.Ignore(c => c.ValidatorId);

        builder.HasQueryFilter(c => c.DeletedAt == null);

        builder.HasOne(c => c.Negotiation)
            .WithMany()
            .HasForeignKey(c => c.NegotiationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Vehicle)
            .WithMany()
            .HasForeignKey(c => c.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
