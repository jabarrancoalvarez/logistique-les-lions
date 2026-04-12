using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Tasa aduanera por par de países y código HS.</summary>
public class CustomsTariff : AuditableEntity
{
    public string OriginCountry { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    /// <summary>Código arancelario HS (ej: 8703 para turismos)</summary>
    public string HsCode { get; set; } = "8703";
    /// <summary>Tasa en % (0 para intra-UE)</summary>
    public decimal TariffRatePercent { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public string? Source { get; set; }
}
