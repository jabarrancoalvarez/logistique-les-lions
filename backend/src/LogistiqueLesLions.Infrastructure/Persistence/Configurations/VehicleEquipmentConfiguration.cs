using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class VehicleEquipmentConfiguration : IEntityTypeConfiguration<VehicleEquipment>
{
    /// <summary>
    /// Catálogo inicial de equipamiento tomado de la especificación funcional.
    /// Los IDs son fijos para que el seed sea idempotente entre entornos.
    /// </summary>
    private static readonly (string Id, string Code, string Name)[] Seed =
    [
        ("20000000-0000-0000-0000-000000000001", "CLIMATISATION",   "Climatisation"),
        ("20000000-0000-0000-0000-000000000002", "BLUETOOTH",       "Bluetooth"),
        ("20000000-0000-0000-0000-000000000003", "GPS",             "Navigation / GPS"),
        ("20000000-0000-0000-0000-000000000004", "CAMERA_RECUL",    "Caméra de recul"),
        ("20000000-0000-0000-0000-000000000005", "RADAR_STATION",   "Radar de stationnement"),
        ("20000000-0000-0000-0000-000000000006", "TOIT_OUVRANT",    "Toit ouvrant"),
        ("20000000-0000-0000-0000-000000000007", "INTERIEUR_CUIR",  "Intérieur cuir"),
        ("20000000-0000-0000-0000-000000000008", "ISOFIX",          "ISOFIX"),
        ("20000000-0000-0000-0000-000000000009", "PHARES_LED",      "Phares LED"),
        ("20000000-0000-0000-0000-000000000010", "REGULATEUR",      "Régulateur de vitesse"),
        ("20000000-0000-0000-0000-000000000011", "JANTES_ALLIAGE",  "Jantes alliage"),
        ("20000000-0000-0000-0000-000000000012", "VITRES_ELEC",     "Vitres électriques"),
        ("20000000-0000-0000-0000-000000000013", "VERROUILLAGE",    "Verrouillage centralisé"),
        ("20000000-0000-0000-0000-000000000014", "ABS",             "ABS"),
        ("20000000-0000-0000-0000-000000000015", "AIRBAGS",         "Airbags"),
        ("20000000-0000-0000-0000-000000000016", "DIRECTION_ASSIST","Direction assistée")
    ];

    public void Configure(EntityTypeBuilder<VehicleEquipment> builder)
    {
        builder.ToTable("vehicle_equipments", "vehicles");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();

        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Timestamp fijo: si se usara DateTimeOffset.UtcNow, cada `migrations add`
        // generaría un UpdateData espurio sobre estas filas.
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(Seed.Select((e, i) => new VehicleEquipment
        {
            Id           = Guid.Parse(e.Id),
            Code         = e.Code,
            Name         = e.Name,
            DisplayOrder = i + 1,
            IsActive     = true,
            CreatedAt    = seededAt,
            UpdatedAt    = seededAt
        }));
    }
}

public class VehicleEquipmentLinkConfiguration : IEntityTypeConfiguration<VehicleEquipmentLink>
{
    public void Configure(EntityTypeBuilder<VehicleEquipmentLink> builder)
    {
        builder.ToTable("vehicle_equipment_links", "vehicles");

        builder.HasKey(l => new { l.VehicleId, l.EquipmentId });

        builder.HasOne(l => l.Vehicle)
            .WithMany(v => v.Equipments)
            .HasForeignKey(l => l.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Equipment)
            .WithMany(e => e.Vehicles)
            .HasForeignKey(l => l.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // El filtro multi-selección del Marketplace busca por equipamiento.
        builder.HasIndex(l => l.EquipmentId);
    }
}
