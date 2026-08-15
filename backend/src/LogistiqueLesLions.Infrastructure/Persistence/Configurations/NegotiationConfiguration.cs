using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class NegotiationConfiguration : IEntityTypeConfiguration<Negotiation>
{
    public void Configure(EntityTypeBuilder<Negotiation> builder)
    {
        builder.ToTable("negotiations", "messaging");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(n => n.Buyer)
            .WithMany()
            .HasForeignKey(n => n.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Seller)
            .WithMany()
            .HasForeignKey(n => n.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Vehicle)
            .WithMany()
            .HasForeignKey(n => n.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Una sola negociación por trío interesado ↔ anuncio ↔ vendedor.
        builder.HasIndex(n => new { n.BuyerId, n.SellerId, n.VehicleId }).IsUnique();
        // El listado agrupa por estado y ordena por actividad.
        builder.HasIndex(n => new { n.Status, n.LastActivityAt });

        // `Involves` es un método, no una propiedad: EF ya lo ignora por sí solo.
        builder.HasQueryFilter(n => n.DeletedAt == null);

        builder.HasMany(n => n.Events)
            .WithOne(e => e.Negotiation)
            .HasForeignKey(e => e.NegotiationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NegotiationEventConfiguration : IEntityTypeConfiguration<NegotiationEvent>
{
    public void Configure(EntityTypeBuilder<NegotiationEvent> builder)
    {
        builder.ToTable("negotiation_events", "messaging");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Amount).HasPrecision(12, 2);

        // La cronología se lee siempre completa y en orden.
        builder.HasIndex(e => new { e.NegotiationId, e.Sequence });

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers", "messaging");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Amount).HasPrecision(12, 2);
        builder.Property(o => o.ListedPrice).HasPrecision(12, 2);
        builder.Property(o => o.Message).HasMaxLength(2000);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(o => new { o.NegotiationId, o.CreatedAt });
        // Localizar la oferta pendiente de una negociación es la consulta más frecuente.
        builder.HasIndex(o => new { o.NegotiationId, o.Status });

        builder.Ignore(o => o.IsPending);
        builder.HasQueryFilter(o => o.DeletedAt == null);

        builder.HasOne(o => o.Negotiation)
            .WithMany(n => n.Offers)
            .HasForeignKey(o => o.NegotiationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.From)
            .WithMany()
            .HasForeignKey(o => o.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
