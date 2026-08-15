using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    // Timestamp fijo: con DateTimeOffset.UtcNow cada `migrations add` generaría un
    // UpdateData espurio sobre estas filas.
    internal static readonly DateTimeOffset SeededAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<PlatformSettings> builder)
    {
        builder.ToTable("platform_settings", "users");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.LegalTermsVersion).HasMaxLength(20).IsRequired();

        builder.HasQueryFilter(s => s.DeletedAt == null);

        builder.HasData(new PlatformSettings
        {
            Id                    = PlatformSettings.SingletonId,
            ComparatorMaxVehicles = 3,
            PointsPerVerifiedSale = 100,
            ListingFreshnessDays  = 60,
            MaxImagesPerListing   = 20,
            LegalTermsVersion     = "1.0",
            CreatedAt             = SeededAt,
            UpdatedAt             = SeededAt
        });
    }
}

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags", "users");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Key).HasMaxLength(60).IsRequired();
        builder.Property(f => f.Label).HasMaxLength(120).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(500);

        // La clave es el contrato con el código: no puede haber dos.
        builder.HasIndex(f => f.Key).IsUnique();

        builder.HasQueryFilter(f => f.DeletedAt == null);

        var at = PlatformSettingsConfiguration.SeededAt;

        builder.HasData(
            Flag("30000001-0000-0000-0000-000000000001", FeatureFlagKeys.PriceIndicator,
                "Indicateur de prix",
                "Affiche « Bonne affaire / Prix correct / Prix élevé » sur les annonces.", 1, at),
            Flag("30000001-0000-0000-0000-000000000002", FeatureFlagKeys.VehicleValuation,
                "Valeur estimée",
                "Estimation statistique de la valeur des véhicules de Mon Garage.", 2, at),
            Flag("30000001-0000-0000-0000-000000000003", FeatureFlagKeys.Comparator,
                "Comparateur",
                "Comparaison de plusieurs véhicules côte à côte.", 3, at),
            Flag("30000001-0000-0000-0000-000000000004", FeatureFlagKeys.VehicleRequests,
                "Trouvez-moi cette voiture",
                "Demandes d'importation gérées par l'équipe.", 4, at),
            Flag("30000001-0000-0000-0000-000000000005", FeatureFlagKeys.UpcomingFeatures,
                "Prochainement",
                "Fonctionnalités à venir et bouton « Ça m'intéresse ».", 5, at));
    }

    private static FeatureFlag Flag(
        string id, string key, string label, string description, int order, DateTimeOffset at) =>
        new()
        {
            Id = Guid.Parse(id), Key = key, Label = label,
            Description = description, IsEnabled = true,
            CreatedAt = at, UpdatedAt = at
        };
}

public class UpcomingFeatureConfiguration : IEntityTypeConfiguration<UpcomingFeature>
{
    public void Configure(EntityTypeBuilder<UpcomingFeature> builder)
    {
        builder.ToTable("upcoming_features", "users");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Code).HasMaxLength(40).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(120).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(500);

        builder.HasIndex(f => f.Code).IsUnique();

        builder.HasQueryFilter(f => f.DeletedAt == null);

        var at = PlatformSettingsConfiguration.SeededAt;

        // El catálogo del documento, para que la pantalla nazca con algo que medir.
        builder.HasData(
            Feature("30000002-0000-0000-0000-000000000001", "STOCK", "Gestion de stock",
                "Gérer un parc de véhicules et publier plusieurs annonces d'un coup.", 1, at),
            Feature("30000002-0000-0000-0000-000000000002", "WHATSAPP", "WhatsApp Business",
                "Recevoir les messages des acheteurs directement sur WhatsApp.", 2, at),
            Feature("30000002-0000-0000-0000-000000000003", "CRM", "CRM",
                "Suivre ses contacts, ses relances et ses ventes.", 3, at),
            Feature("30000002-0000-0000-0000-000000000004", "TENDANCES", "Tendances du marché",
                "Voir l'évolution des prix et de la demande par modèle.", 4, at),
            Feature("30000002-0000-0000-0000-000000000005", "OUTILS", "Outils intelligents",
                "Aide à la fixation du prix et à la rédaction des annonces.", 5, at));
    }

    private static UpcomingFeature Feature(
        string id, string code, string name, string description, int order, DateTimeOffset at) =>
        new()
        {
            Id = Guid.Parse(id), Code = code, Name = name,
            Description = description, DisplayOrder = order, IsActive = true,
            CreatedAt = at, UpdatedAt = at
        };
}

public class FeatureInterestConfiguration : IEntityTypeConfiguration<FeatureInterest>
{
    public void Configure(EntityTypeBuilder<FeatureInterest> builder)
    {
        builder.ToTable("feature_interests", "users");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.HasQueryFilter(i => i.DeletedAt == null);

        // Pulsar dos veces no vale por dos. Parcial, porque retirar el interés es un
        // soft delete: sin el filtro, quien lo retira no podría volver a declararlo.
        // ⚠️ El proveedor en memoria de los tests no valida los índices parciales.
        builder.HasIndex(i => new { i.FeatureId, i.UserId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(i => i.Feature)
            .WithMany(f => f.Interests)
            .HasForeignKey(i => i.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LoyaltyPointEntryConfiguration : IEntityTypeConfiguration<LoyaltyPointEntry>
{
    public void Configure(EntityTypeBuilder<LoyaltyPointEntry> builder)
    {
        builder.ToTable("loyalty_point_entries", "users");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Origin).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.ContractReference).HasMaxLength(20);
        builder.Property(e => e.Note).HasMaxLength(1000);

        // El libro se lee por persona y en orden cronológico.
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        // Append-only: sin filtro de soft delete porque nunca se borra un movimiento.
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Admin)
            .WithMany()
            .HasForeignKey(e => e.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
