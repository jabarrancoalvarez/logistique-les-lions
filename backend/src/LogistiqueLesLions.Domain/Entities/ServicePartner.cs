using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Partner del marketplace de servicios (gestores, transportistas, inspectores, etc.)
/// que la plataforma ofrece a sus usuarios.
/// </summary>
public class ServicePartner : AuditableEntity
{
    public PartnerType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Códigos ISO de países donde opera, separados por coma (ES,FR,DE).</summary>
    public string CountriesCsv { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>Rating medio 0..5 (calculado externamente / manual al inicio).</summary>
    public decimal Rating { get; set; }
    public int ReviewsCount { get; set; }

    /// <summary>Coste base orientativo en EUR (puede ser null si depende de cotización).</summary>
    public decimal? BasePriceEur { get; set; }

    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
}
