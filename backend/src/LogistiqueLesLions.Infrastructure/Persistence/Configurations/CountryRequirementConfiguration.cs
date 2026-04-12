using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class CountryRequirementConfiguration : IEntityTypeConfiguration<CountryRequirement>
{
    public void Configure(EntityTypeBuilder<CountryRequirement> builder)
    {
        builder.ToTable("country_requirements", "compliance");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.OriginCountry).HasMaxLength(2).IsRequired();
        builder.Property(r => r.DestinationCountry).HasMaxLength(2).IsRequired();
        builder.Property(r => r.DocumentTypesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.RoadTaxFormula).HasColumnType("jsonb");
        builder.Property(r => r.CustomsRatePercent).HasPrecision(5, 2);
        builder.Property(r => r.VatRatePercent).HasPrecision(5, 2);
        builder.Property(r => r.EstimatedProcessingCostEur).HasPrecision(10, 2);
        builder.Property(r => r.NotesEs).HasMaxLength(2000);
        builder.Property(r => r.NotesEn).HasMaxLength(2000);

        builder.HasIndex(r => new { r.OriginCountry, r.DestinationCountry }).IsUnique();
        builder.HasQueryFilter(r => r.DeletedAt == null);

        SeedData(builder);
    }

    private static void SeedData(EntityTypeBuilder<CountryRequirement> builder)
    {
        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            // ─── Intra-UE (sin aranceles, sin IVA a la importación) ──────────
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000001"),
                OriginCountry = "ES", DestinationCountry = "DE",
                DocumentTypesJson = """["FichaTecnica","COC","TituloPropiedad","Itv"]""",
                HomologationRequired = false, CustomsRatePercent = 0, VatRatePercent = 0,
                TechnicalInspectionRequired = false, EstimatedProcessingCostEur = 300,
                EstimatedDays = 15, LastUpdatedAt = now,
                NotesEs = "Transferencia intra-UE. Sin aranceles. COC europeo requerido."
            },
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000002"),
                OriginCountry = "DE", DestinationCountry = "ES",
                DocumentTypesJson = """["FichaTecnica","COC","TituloPropiedad"]""",
                HomologationRequired = false, CustomsRatePercent = 0, VatRatePercent = 0,
                TechnicalInspectionRequired = false, EstimatedProcessingCostEur = 280,
                EstimatedDays = 10, LastUpdatedAt = now,
                NotesEs = "Transferencia intra-UE. ITV en España obligatoria al matricular."
            },
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000003"),
                OriginCountry = "DE", DestinationCountry = "FR",
                DocumentTypesJson = """["FichaTecnica","COC","TituloPropiedad"]""",
                HomologationRequired = false, CustomsRatePercent = 0, VatRatePercent = 0,
                TechnicalInspectionRequired = false, EstimatedProcessingCostEur = 250,
                EstimatedDays = 10, LastUpdatedAt = now,
                NotesEs = "Transferencia intra-UE Francia. Control técnico (CT) requerido al matricular."
            },
            // ─── Importación extra-UE → España ──────────────────────────────
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000004"),
                OriginCountry = "US", DestinationCountry = "ES",
                DocumentTypesJson = """["TituloPropiedad","DeclaracionAduana","PagoAranceles","Homologacion","Itv","SeguroImportacion"]""",
                HomologationRequired = true, CustomsRatePercent = 6.5m, VatRatePercent = 21,
                TechnicalInspectionRequired = true, EstimatedProcessingCostEur = 1500,
                EstimatedDays = 45, LastUpdatedAt = now,
                NotesEs = "Arancel UE 6.5% sobre valor CIF. IVA 21% sobre valor + arancel. Homologación obligatoria para vehículos no certificados ECE."
            },
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000005"),
                OriginCountry = "JP", DestinationCountry = "ES",
                DocumentTypesJson = """["TituloPropiedad","DeclaracionAduana","PagoAranceles","Homologacion","Itv","SeguroImportacion"]""",
                HomologationRequired = true, CustomsRatePercent = 6.5m, VatRatePercent = 21,
                TechnicalInspectionRequired = true, EstimatedProcessingCostEur = 2000,
                EstimatedDays = 60, LastUpdatedAt = now,
                NotesEs = "Importación desde Japón. Requiere homologación y adaptación de luces/faros al estándar europeo."
            },
            // ─── Exportación extra-UE desde España ─────────────────────────
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000006"),
                OriginCountry = "ES", DestinationCountry = "MA",
                DocumentTypesJson = """["TituloPropiedad","FichaTecnica","DeclaracionAduana","SeguroImportacion","InspeccionTecnica"]""",
                HomologationRequired = true, CustomsRatePercent = 2.5m, VatRatePercent = 20,
                TechnicalInspectionRequired = true, EstimatedProcessingCostEur = 800,
                EstimatedDays = 30, LastUpdatedAt = now,
                NotesEs = "Exportación a Marruecos. Requiere vistazos en aduana de Melilla/Ceuta o flete marítimo. Vehículo debe tener menos de 5 años."
            },
            new CountryRequirement
            {
                Id = Guid.Parse("10000001-0000-0000-0000-000000000007"),
                OriginCountry = "ES", DestinationCountry = "GB",
                DocumentTypesJson = """["TituloPropiedad","FichaTecnica","DeclaracionAduana","MOT","SeguroImportacion"]""",
                HomologationRequired = true, CustomsRatePercent = 6.5m, VatRatePercent = 20,
                TechnicalInspectionRequired = true, EstimatedProcessingCostEur = 1200,
                EstimatedDays = 30, LastUpdatedAt = now,
                NotesEs = "Post-Brexit: se aplican aranceles UE-UK. Conversión a conducción por la izquierda no obligatoria pero faros deben ajustarse."
            }
        );
    }
}
