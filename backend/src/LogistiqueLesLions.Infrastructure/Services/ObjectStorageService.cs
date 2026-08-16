using Amazon.S3;
using Amazon.S3.Model;
using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LogistiqueLesLions.Infrastructure.Services;

/// <summary>
/// Almacenamiento de objetos compatible con S3. Pensado para <b>Cloudflare R2</b>, pero
/// sirve igual con AWS S3, Backblaze B2 o MinIO: todos hablan el mismo protocolo.
/// </summary>
/// <remarks>
/// Sustituye a <see cref="LocalStorageService"/> en producción porque el disco de Render
/// es <b>efímero</b>: se recrea en cada despliegue y, en el plan gratuito, tras un rato de
/// inactividad. Con el disco local, las fotografías de los anuncios y la documentación de
/// Mon Garage desaparecían y la ficha quedaba con filas que apuntaban a nada.
///
/// Los archivos no van a PostgreSQL a propósito: Neon da medio giga en plan gratuito, una
/// foto ronda los 300 kB, y meterlos ahí engorda además cada copia de seguridad.
///
/// <para>
/// <b>Público y privado en el mismo bucket.</b> Se separan por prefijo:
/// <list type="bullet">
///   <item><c>public/…</c> — fotos de anuncios. Se sirven por la URL pública del bucket.</item>
///   <item><c>private/…</c> — documentos y fotos de Mon Garage. <b>No tienen URL</b>: solo
///   se leen desde el servidor, después de comprobar de quién son.</item>
/// </list>
/// El bucket debe estar cerrado al público salvo el prefijo <c>public/</c>. Si se expone
/// entero, la documentación privada de Mon Garage queda al alcance de cualquiera.
/// </para>
/// </remarks>
public class ObjectStorageService : IStorageService
{
    private const string PublicPrefix  = "public/";
    private const string PrivatePrefix = "private/";

    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;

    public ObjectStorageService(IConfiguration configuration)
    {
        _bucket = configuration["Storage:Bucket"]
            ?? throw new InvalidOperationException("Falta Storage:Bucket.");

        _publicBaseUrl = (configuration["Storage:PublicBaseUrl"]
            ?? throw new InvalidOperationException("Falta Storage:PublicBaseUrl."))
            .TrimEnd('/');

        var serviceUrl = configuration["Storage:ServiceUrl"]
            ?? throw new InvalidOperationException("Falta Storage:ServiceUrl.");

        _client = new AmazonS3Client(
            configuration["Storage:AccessKey"],
            configuration["Storage:SecretKey"],
            new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                // R2 no tiene regiones al estilo de AWS y exige el estilo de ruta.
                ForcePathStyle = true,
                // Firma la petición sin pedir la región al proveedor.
                AuthenticationRegion = configuration["Storage:Region"] ?? "auto"
            });
    }

    /// <summary>Nombre irrepetible conservando la extensión, que da el tipo al navegador.</summary>
    private static string SafeName(string fileName) =>
        $"{Guid.NewGuid()}{Path.GetExtension(fileName).ToLowerInvariant()}";

    private async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName  = _bucket,
            Key         = key,
            InputStream = content,
            ContentType = contentType,
            // Sin AutoCloseStream: el flujo lo cierra quien lo abrió.
            AutoCloseStream = false
        }, ct);
    }

    // ─── Archivos públicos ─────────────────────────────────────────────────

    public async Task<(string Url, string? ThumbnailUrl)> UploadAsync(
        Stream content, string fileName, string contentType, string folder,
        CancellationToken ct = default)
    {
        var key = $"{PublicPrefix}{folder.Trim('/')}/{SafeName(fileName)}";
        await PutAsync(key, content, contentType, ct);

        // Sin miniatura: generarla exigiría procesar la imagen, y el listado ya limita el
        // tamaño mostrado. Se devuelve null como hace el almacenamiento local.
        return ($"{_publicBaseUrl}/{key}", null);
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith(_publicBaseUrl, StringComparison.OrdinalIgnoreCase)) return;

        var key = url[_publicBaseUrl.Length..].TrimStart('/');
        if (key.Length == 0) return;

        await _client.DeleteObjectAsync(_bucket, key, ct);
    }

    // ─── Archivos privados ─────────────────────────────────────────────────

    public async Task<string> UploadPrivateAsync(
        Stream content, string fileName, string contentType, string folder,
        CancellationToken ct = default)
    {
        // La clave que se guarda es relativa al prefijo privado: si mañana cambia el
        // bucket o el proveedor, las filas de la base de datos siguen valiendo.
        var relative = $"{folder.Trim('/')}/{SafeName(fileName)}";
        await PutAsync(PrivatePrefix + relative, content, contentType, ct);
        return relative;
    }

    public async Task<Stream?> OpenPrivateAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, PrivatePrefix + key, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Igual que en disco: un archivo que ya no está devuelve null, no revienta.
            return null;
        }
    }

    public async Task DeletePrivateAsync(string key, CancellationToken ct = default) =>
        await _client.DeleteObjectAsync(_bucket, PrivatePrefix + key, ct);

    public async Task<(string Url, string? ThumbnailUrl)?> PublishPrivateAsync(
        string key, string fileName, string contentType, string folder,
        CancellationToken ct = default)
    {
        await using var origen = await OpenPrivateAsync(key, ct);
        if (origen is null) return null;

        // Se copia, no se mueve: el original tiene que seguir en el garaje aunque el
        // anuncio se retire después.
        return await UploadAsync(origen, fileName, contentType, folder, ct);
    }
}
