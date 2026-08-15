using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Contrato de compraventa («Contrat de vente») de una negociación.
/// </summary>
/// <remarks>
/// Los datos del vehículo y de las partes se <b>congelan</b> al crear el contrato en
/// lugar de leerse por referencia: un contrato debe reflejar lo acordado en su momento,
/// y si el anuncio cambia de precio o de kilometraje después, el documento firmado no
/// puede cambiar con él.
/// </remarks>
public class Contract : AuditableEntity
{
    /// <summary>Referencia pública única: "YC00125".</summary>
    public string PublicReference { get; set; } = string.Empty;

    public Guid NegotiationId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }

    /// <summary>Quién creó el contrato: la otra parte es quien debe validarlo.</summary>
    public Guid CreatedByUserId { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Brouillon;

    // ─── Datos del vehículo, congelados ────────────────────────────────────
    public string VehicleMake { get; set; } = string.Empty;
    public string? VehicleModel { get; set; }
    public string? VehicleVersion { get; set; }
    public int VehicleYear { get; set; }
    public int? VehicleMileage { get; set; }
    public string? VehicleVin { get; set; }
    /// <summary>Matrícula. Suele faltar en el anuncio y se pide al crear el contrato.</summary>
    public string? RegistrationPlate { get; set; }
    /// <summary>Referencia del anuncio de origen.</summary>
    public string VehicleReference { get; set; } = string.Empty;

    // ─── Datos de la operación ─────────────────────────────────────────────
    /// <summary>Precio acordado en FCFA.</summary>
    public decimal AgreedPrice { get; set; }
    public DateTimeOffset SaleDate { get; set; } = DateTimeOffset.UtcNow;

    // ─── Datos legales de las partes ───────────────────────────────────────
    public string SellerLegalName { get; set; } = string.Empty;
    /// <summary>CNI o pasaporte. Se solicita al crear el contrato si no consta.</summary>
    public string? SellerIdDocument { get; set; }
    public string? SellerAddress { get; set; }

    public string BuyerLegalName { get; set; } = string.Empty;
    public string? BuyerIdDocument { get; set; }
    public string? BuyerAddress { get; set; }

    /// <summary>
    /// Código del QR de verificación. Se genera al validar el contrato.
    /// </summary>
    /// <remarks>
    /// Es la credencial que permite comprobar públicamente que el papel que alguien
    /// tiene en la mano corresponde a una venta real: por eso es aleatorio y no
    /// derivado de la referencia, que sí es adivinable.
    /// </remarks>
    public string? VerificationCode { get; set; }

    // ─── Ciclo de vida ─────────────────────────────────────────────────────
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>Qué pidió corregir quien debía validar.</summary>
    public string? ChangeRequestNotes { get; set; }
    public DateTimeOffset? ChangeRequestedAt { get; set; }

    public Negotiation Negotiation { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>Un contrato validado ya no puede modificarse.</summary>
    public bool IsEditable =>
        Status is ContractStatus.Brouillon or ContractStatus.ModificationDemandee;

    /// <summary>Espera la validación de la otra parte.</summary>
    public bool AwaitsValidation => Status == ContractStatus.AValider;

    /// <summary>Quien debe validarlo: siempre la parte que no lo creó.</summary>
    public Guid ValidatorId => CreatedByUserId == SellerId ? BuyerId : SellerId;
}
