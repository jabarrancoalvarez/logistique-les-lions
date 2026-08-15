namespace LogistiqueLesLions.Application.Common.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Sube un archivo y devuelve (url, thumbnailUrl).
    /// thumbnailUrl puede ser null si no se genera miniatura.
    /// </summary>
    Task<(string Url, string? ThumbnailUrl)> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    Task DeleteAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Sube un archivo <b>privado</b> y devuelve una clave opaca, nunca una URL.
    /// </summary>
    /// <remarks>
    /// Los archivos subidos con <see cref="UploadAsync"/> se sirven de forma estática:
    /// quien conozca la URL los abre sin autenticarse. Eso vale para las fotografías de
    /// un anuncio, pero no para la documentación de Mon Garage, que puede contener datos
    /// personales. Estos archivos quedan fuera del directorio público y solo se leen con
    /// <see cref="OpenPrivateAsync"/>, tras comprobar quién los pide.
    /// </remarks>
    Task<string> UploadPrivateAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>Abre un archivo privado. <c>null</c> si ya no existe.</summary>
    Task<Stream?> OpenPrivateAsync(string key, CancellationToken ct = default);

    Task DeletePrivateAsync(string key, CancellationToken ct = default);
}
