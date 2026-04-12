using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LogistiqueLesLions.Infrastructure.Services;

/// <summary>
/// Almacenamiento local en disco para desarrollo.
/// En producción, sustituir por Cloudflare R2 / AWS S3.
/// </summary>
public class LocalStorageService(IConfiguration configuration) : IStorageService
{
    private readonly string _basePath = configuration["Storage:LocalPath"] ?? "uploads";
    private readonly string _baseUrl  = configuration["Storage:BaseUrl"]  ?? "http://localhost:5000/uploads";

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

        var url = $"{_baseUrl}/{folder}/{safeName}";
        return (url, null); // thumbnail generation requires SixLabors.ImageSharp (optional)
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        // Derive local path from URL
        var relative = url.Replace(_baseUrl, "").TrimStart('/');
        var filePath = Path.Combine(_basePath, relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}
