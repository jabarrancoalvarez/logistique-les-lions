using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Vehículo propuesto por el administrador para una solicitud.
/// </summary>
/// <remarks>
/// Puede ser un anuncio ya publicado en Yoon u Auto —en cuyo caso el usuario abre la
/// ficha directamente— o una propuesta externa de importación introducida a mano.
/// La creación corresponde al backoffice (parte P29); aquí solo se define la estructura
/// y el usuario la consulta desde su solicitud.
/// </remarks>
public class VehicleRequestProposal : AuditableEntity
{
    public Guid RequestId { get; set; }

    /// <summary>Anuncio de Yoon u Auto, si la propuesta es interna.</summary>
    public Guid? VehicleId { get; set; }

    // ─── Propuesta externa ─────────────────────────────────────────────────
    public string? MakeModel { get; set; }
    /// <summary>Versión/acabado del vehículo encontrado.</summary>
    public string? Version { get; set; }
    public int? Year { get; set; }
    public int? Mileage { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    /// <summary>Precio estimado en FCFA.</summary>
    public decimal? EstimatedPrice { get; set; }
    /// <summary>
    /// Costes adicionales conocidos: transporte, aduana… Solo si se conocen.
    /// </summary>
    /// <remarks>
    /// Van aparte del precio a propósito: quien pide un coche debe ver qué es el
    /// vehículo y qué es lo que costará traerlo.
    /// </remarks>
    public decimal? AdditionalCosts { get; set; }
    public string? CountryOfOrigin { get; set; }
    /// <summary>URLs de fotografías, separadas por salto de línea.</summary>
    public string? PhotoUrls { get; set; }
    public string? ExternalUrl { get; set; }
    public string? Comments { get; set; }

    /// <summary>El usuario ya ha visto la propuesta.</summary>
    public bool IsSeenByUser { get; set; }

    public VehicleRequest Request { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }

    /// <summary>Propuesta de un anuncio publicado en la plataforma.</summary>
    public bool IsInternal => VehicleId is not null;
}
