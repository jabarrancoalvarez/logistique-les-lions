using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Documento del historial documental de un vehículo de Mon Garage.
/// </summary>
/// <remarks>
/// La especificación es tajante: <b>toda la documentación es privada por defecto y
/// ningún otro usuario puede acceder a ella</b>. Por eso el fichero no se guarda en el
/// almacenamiento público de las fotografías, sino en el privado, y aquí solo consta
/// una clave opaca: el archivo se sirve por un endpoint que comprueba quién pide.
/// </remarks>
public class GarageDocument : AuditableEntity
{
    public Guid GarageVehicleId { get; set; }

    public GarageDocumentType Type { get; set; } = GarageDocumentType.Autre;

    /// <summary>Nombre que le da el usuario: «Carte grise 2024».</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Fecha del documento, que no tiene por qué ser la de subida: una factura de 2019
    /// puede subirse hoy. Es la que ordena el historial.
    /// </summary>
    public DateTimeOffset? DocumentDate { get; set; }

    /// <summary>
    /// Clave en el almacenamiento privado. ❌ Nunca se expone en la API: el fichero se
    /// descarga por un endpoint autenticado.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Nombre original del archivo, para devolverlo al descargar.</summary>
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public string? Notes { get; set; }

    public GarageVehicle GarageVehicle { get; set; } = null!;
}
