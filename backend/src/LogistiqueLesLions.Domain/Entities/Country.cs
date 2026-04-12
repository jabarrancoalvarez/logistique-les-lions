using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>País soportado por la plataforma con su configuración de importación/exportación.</summary>
public class Country : AuditableEntity
{
    /// <summary>Código ISO 3166-1 alpha-2 (ES, DE, FR...)</summary>
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? FlagEmoji { get; set; }
    /// <summary>Código de moneda ISO 4217</summary>
    public string Currency { get; set; } = "EUR";
    /// <summary>Pertenece a la UE (diferentes reglas aduaneras)</summary>
    public bool IsEuMember { get; set; }
    /// <summary>La plataforma gestiona importaciones desde este país</summary>
    public bool SupportsImport { get; set; }
    /// <summary>La plataforma gestiona exportaciones hacia este país</summary>
    public bool SupportsExport { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Orden de aparición en listas</summary>
    public int DisplayOrder { get; set; }
}
