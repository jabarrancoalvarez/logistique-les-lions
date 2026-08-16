using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogistiqueLesLions.Infrastructure.Services;

/// <summary>
/// Guarda los archivos dentro de la propia base de datos (<see cref="StoredFile"/>).
/// </summary>
/// <remarks>
/// Pensado para la demo: el disco de Render es efímero y montar Cloudflare R2 exige cuenta
/// y claves, así que los bytes viven en Neon y sobreviven a los despliegues sin ningún
/// servicio externo. Se activa con <c>Storage:Provider=database</c>.
///
/// Las fotos públicas se sirven por <c>GET /files/{clave}</c>, un endpoint que lee la fila
/// y devuelve el binario —lo estático de disco aquí no vale, porque no hay disco—. La
/// documentación privada sigue el mismo camino que con los otros proveedores:
/// <see cref="OpenPrivateAsync"/>, tras comprobar quién la pide.
/// </remarks>
public class DatabaseStorageService(
    IApplicationDbContext db,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor) : IStorageService
{
    private readonly string? _configuredBaseUrl = configuration["Storage:BaseUrl"];

    /// <summary>Raíz pública desde la que se sirven los archivos: <c>.../files</c>.</summary>
    private string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_configuredBaseUrl))
            return _configuredBaseUrl.TrimEnd('/');

        var req = httpContextAccessor.HttpContext?.Request;
        if (req is not null)
            return $"{req.Scheme}://{req.Host}/files";

        return "http://localhost:5000/files";
    }

    private static string BuildKey(string folder, string fileName)
    {
        var ext      = Path.GetExtension(fileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid()}{ext}";
        return $"{folder}/{safeName}";
    }

    private static async Task<byte[]> ReadAllAsync(Stream content, CancellationToken ct)
    {
        if (content is MemoryStream ms) return ms.ToArray();

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    public async Task<(string Url, string? ThumbnailUrl)> UploadAsync(
        Stream content, string fileName, string contentType, string folder,
        CancellationToken ct = default)
    {
        var key   = BuildKey(folder, fileName);
        var bytes = await ReadAllAsync(content, ct);

        db.StoredFiles.Add(new StoredFile
        {
            StorageKey  = key,
            FileName    = fileName,
            ContentType = contentType,
            Content     = bytes,
            SizeBytes   = bytes.LongLength,
            IsPublic    = true
        });
        await db.SaveChangesAsync(ct);

        // La miniatura se deja en null, igual que el resto de proveedores: generarla exige
        // una librería de imagen que hoy no se usa.
        return ($"{ResolveBaseUrl()}/{key}", null);
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var key = KeyFromUrl(url);
        if (key is null) return;

        await BorrarAsync(key, publico: true, ct);
    }

    /// <summary>Recupera la clave a partir de la URL pública, sea absoluta o relativa.</summary>
    private string? KeyFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var baseUrl = ResolveBaseUrl();
        if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            return url[baseUrl.Length..].TrimStart('/');

        // Sirve también cuando llega la ruta suelta «/files/carpeta/archivo».
        var marca = "/files/";
        var i = url.IndexOf(marca, StringComparison.OrdinalIgnoreCase);
        return i >= 0 ? url[(i + marca.Length)..] : null;
    }

    // ─── Archivos privados ─────────────────────────────────────────────────

    public async Task<string> UploadPrivateAsync(
        Stream content, string fileName, string contentType, string folder,
        CancellationToken ct = default)
    {
        var key   = BuildKey(folder, fileName);
        var bytes = await ReadAllAsync(content, ct);

        db.StoredFiles.Add(new StoredFile
        {
            StorageKey  = key,
            FileName    = fileName,
            ContentType = contentType,
            Content     = bytes,
            SizeBytes   = bytes.LongLength,
            IsPublic    = false
        });
        await db.SaveChangesAsync(ct);

        return key;
    }

    public async Task<Stream?> OpenPrivateAsync(string key, CancellationToken ct = default)
    {
        // Solo privados: una clave privada nunca debe poder abrir una foto pública ni al
        // revés. El binario se materializa entero —son documentos pequeños— y se envuelve
        // en un MemoryStream para que el endpoint lo entregue.
        var bytes = await db.StoredFiles
            .AsNoTracking()
            .Where(f => f.StorageKey == key && !f.IsPublic)
            .Select(f => f.Content)
            .FirstOrDefaultAsync(ct);

        return bytes is null ? null : new MemoryStream(bytes, writable: false);
    }

    public async Task DeletePrivateAsync(string key, CancellationToken ct = default)
    {
        await BorrarAsync(key, publico: false, ct);
    }

    /// <summary>
    /// Borra por clave. Se carga la fila y se elimina, en vez de enganchar un señuelo por
    /// Id: el señuelo choca con la instancia que el contexto ya pueda estar rastreando.
    /// Borrar un archivo es una operación rara, así que traer sus bytes una vez no importa.
    /// </summary>
    private async Task BorrarAsync(string key, bool publico, CancellationToken ct)
    {
        var fila = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.StorageKey == key && f.IsPublic == publico, ct);

        if (fila is null) return;

        db.StoredFiles.Remove(fila);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(string Url, string? ThumbnailUrl)?> PublishPrivateAsync(
        string key, string fileName, string contentType, string folder,
        CancellationToken ct = default)
    {
        var bytes = await db.StoredFiles
            .AsNoTracking()
            .Where(f => f.StorageKey == key && !f.IsPublic)
            .Select(f => f.Content)
            .FirstOrDefaultAsync(ct);

        if (bytes is null) return null;

        // Se copia, no se mueve: el original tiene que seguir en el garaje aunque el
        // anuncio se retire después.
        using var copia = new MemoryStream(bytes, writable: false);
        return await UploadAsync(copia, fileName, contentType, folder, ct);
    }
}
