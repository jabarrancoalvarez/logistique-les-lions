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
}
