using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LogistiqueLesLions.Infrastructure.Services;

/// <summary>
/// Almacenamiento local en disco para desarrollo.
/// En producción, sustituir por Cloudflare R2 / AWS S3.
/// </summary>
public class LocalStorageService(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor) : IStorageService
{
    private readonly string _basePath = configuration["Storage:LocalPath"] ?? "uploads";
    private readonly string? _configuredBaseUrl = configuration["Storage:BaseUrl"];
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_configuredBaseUrl))
            return _configuredBaseUrl.TrimEnd('/');

        var req = _httpContextAccessor.HttpContext?.Request;
        if (req is not null)
            return $"{req.Scheme}://{req.Host}/uploads";

        return "http://localhost:5000/uploads";
    }

    public async Task<(string Url, string? ThumbnailUrl)> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var ext      = Path.GetExtension(fileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid()}{ext}";
        var dir      = Path.Combine(_basePath, folder);

        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, safeName);
        await using var fs = File.Create(filePath);
        await content.CopyToAsync(fs, ct);

        var url = $"{ResolveBaseUrl()}/{folder}/{safeName}";
        return (url, null); // thumbnail generation requires SixLabors.ImageSharp (optional)
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        // Derive local path from URL
        var relative = url.Replace(ResolveBaseUrl(), "").TrimStart('/');
        var filePath = Path.Combine(_basePath, relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    // ─── Archivos privados ─────────────────────────────────────────────────
    /// <summary>
    /// Fuera del directorio servido estáticamente: estos archivos no deben tener URL.
    /// </summary>
    private readonly string _privatePath =
        configuration["Storage:PrivatePath"] ?? "private-uploads";

    public async Task<string> UploadPrivateAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var ext      = Path.GetExtension(fileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid()}{ext}";
        var dir      = Path.Combine(_privatePath, folder);

        Directory.CreateDirectory(dir);

        await using var fs = File.Create(Path.Combine(dir, safeName));
        await content.CopyToAsync(fs, ct);

        // La clave es relativa: el almacenamiento puede cambiar de sitio sin tocar la BD.
        return $"{folder}/{safeName}";
    }

    public Task<Stream?> OpenPrivateAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePrivatePath(key);
        if (path is null || !File.Exists(path)) return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task DeletePrivateAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePrivatePath(key);
        if (path is not null && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<(string Url, string? ThumbnailUrl)?> PublishPrivateAsync(
        string key,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        await using var origen = await OpenPrivateAsync(key, ct);
        if (origen is null) return null;

        // Se copia, no se mueve: el original tiene que seguir en el garaje aunque el
        // anuncio se retire después.
        return await UploadAsync(origen, fileName, contentType, folder, ct);
    }

    /// <summary>
    /// Convierte la clave en una ruta, comprobando que no se sale del directorio privado.
    /// </summary>
    /// <remarks>
    /// La clave viene de la base de datos, pero una ruta construida a ciegas es un
    /// camino directo al resto del disco si algún día llega manipulada.
    /// </remarks>
    private string? ResolvePrivatePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var root = Path.GetFullPath(_privatePath);
        var full = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));

        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? full
            : null;
    }
}
