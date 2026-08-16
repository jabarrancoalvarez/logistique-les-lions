namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// El contenido de un archivo guardado dentro de la propia base de datos.
/// </summary>
/// <remarks>
/// Es la alternativa a guardar en disco o en un almacén de objetos. El disco de Render es
/// efímero —lo borra cada despliegue— y montar Cloudflare R2 exige una cuenta y cinco
/// variables. Para la demo, donde entran pocos ejemplos, guardar los bytes en Neon hace
/// que las fotos subidas sobrevivan sin ningún servicio externo. Se activa con
/// <c>Storage:Provider=database</c>.
///
/// No es lo que se hace con un catálogo grande —comparte espacio con los datos y sirve las
/// imágenes más despacio que un CDN—, pero como el almacenamiento vive tras
/// <c>IStorageService</c>, el día que crezca se cambia el proveedor sin tocar nada más.
///
/// <see cref="IsPublic"/> separa las dos naturalezas que ya distinguía el resto del
/// sistema: las fotos de un anuncio se sirven a cualquiera por su clave, y la
/// documentación de Mon Garage solo tras comprobar quién la pide. Nunca se mezclan.
/// </remarks>
public class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruta relativa <c>carpeta/guid.ext</c>. Es lo que se guarda en las tablas de
    /// negocio, nunca el <see cref="Id"/>: así el almacenamiento puede cambiar de sitio.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>El binario. En PostgreSQL es una columna <c>bytea</c>.</summary>
    public byte[] Content { get; set; } = [];

    public long SizeBytes { get; set; }

    /// <summary>
    /// <c>true</c> si se sirve por su clave sin autenticación (foto de anuncio);
    /// <c>false</c> si exige comprobar al solicitante (documento de Mon Garage).
    /// </summary>
    public bool IsPublic { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
