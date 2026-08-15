using System.Text;
using FluentAssertions;
using LogistiqueLesLions.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Storage;

/// <summary>
/// Almacenamiento privado: el que guarda la documentación de Mon Garage.
/// </summary>
/// <remarks>
/// Lo que se comprueba aquí es que estos archivos quedan fuera del directorio que se
/// sirve estáticamente y que una clave manipulada no puede salir de él.
/// </remarks>
public class LocalStorageServiceTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"yoon-storage-{Guid.NewGuid():N}");

    private readonly LocalStorageService _storage;

    public LocalStorageServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:LocalPath"]   = Path.Combine(_root, "uploads"),
                ["Storage:PrivatePath"] = Path.Combine(_root, "private-uploads"),
                ["Storage:BaseUrl"]     = "http://localhost:5000/uploads"
            })
            .Build();

        _storage = new LocalStorageService(configuration, new HttpContextAccessor());
    }

    private static Stream Content(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task LosArchivosPrivadosNoDebenCaerEnElDirectorioPublico()
    {
        var key = await _storage.UploadPrivateAsync(
            Content("carte grise"), "cg.pdf", "application/pdf", "garage/documents");

        // La clave es relativa y opaca: nunca una URL.
        key.Should().StartWith("garage/documents/");
        key.Should().NotContain("http");

        var publicDir = Path.Combine(_root, "uploads");
        (Directory.Exists(publicDir)
            ? Directory.GetFiles(publicDir, "*", SearchOption.AllDirectories)
            : []).Should().BeEmpty();
    }

    [Fact]
    public async Task DeberiaPoderLeerseElArchivoSubido()
    {
        var key = await _storage.UploadPrivateAsync(
            Content("contenu du document"), "doc.pdf", "application/pdf", "garage/documents");

        await using var stream = await _storage.OpenPrivateAsync(key);
        stream.Should().NotBeNull();

        using var reader = new StreamReader(stream!);
        (await reader.ReadToEndAsync()).Should().Be("contenu du document");
    }

    [Fact]
    public async Task UnaClaveQueSeSaleDelDirectorioNoDebeLeerNada()
    {
        // La clave viene de la base de datos, pero una ruta construida a ciegas sería un
        // camino directo al resto del disco.
        var escapes = new[]
        {
            "../uploads/secret.txt",
            "../../etc/passwd",
            "garage/../../appsettings.json"
        };

        foreach (var key in escapes)
            (await _storage.OpenPrivateAsync(key)).Should().BeNull($"«{key}» sale del directorio");
    }

    [Fact]
    public async Task BorrarDebeQuitarElArchivoDelDisco()
    {
        var key = await _storage.UploadPrivateAsync(
            Content("x"), "doc.pdf", "application/pdf", "garage/documents");

        await _storage.DeletePrivateAsync(key);

        (await _storage.OpenPrivateAsync(key)).Should().BeNull();
    }

    [Fact]
    public async Task UnaClaveInexistenteNoDebeReventar()
    {
        (await _storage.OpenPrivateAsync("garage/documents/no-existe.pdf")).Should().BeNull();
        await _storage.DeletePrivateAsync("garage/documents/no-existe.pdf");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
