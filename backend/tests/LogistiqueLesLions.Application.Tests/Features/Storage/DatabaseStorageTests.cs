using System.Text;
using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Infrastructure.Persistence;
using LogistiqueLesLions.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Storage;

/// <summary>
/// Almacenamiento dentro de la propia base (Storage:Provider=database).
/// </summary>
/// <remarks>
/// Lo que hay que garantizar: que un archivo subido se recupera igual, que lo público y lo
/// privado no se cruzan —una clave pública nunca abre por el camino privado ni al revés— y
/// que «Vendre ce véhicule» copia sin vaciar el garaje.
/// </remarks>
public class DatabaseStorageTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DatabaseStorageService _storage;

    public DatabaseStorageTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(currentUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                currentUser.Object, new HttpContextAccessor()));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:BaseUrl"] = "https://demo.test/files"
            })
            .Build();

        _storage = new DatabaseStorageService(_context, config, new HttpContextAccessor());
    }

    private static Stream Bytes(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task DeberiaGuardarYServirUnaFotoPublicaPorSuClave()
    {
        var (url, thumb) = await _storage.UploadAsync(
            Bytes("photo-bytes"), "voiture.jpg", "image/jpeg", "vehicles");

        url.Should().StartWith("https://demo.test/files/vehicles/");
        url.Should().EndWith(".jpg");
        thumb.Should().BeNull();

        var fila = await _context.StoredFiles.SingleAsync();
        fila.IsPublic.Should().BeTrue();
        fila.SizeBytes.Should().Be("photo-bytes".Length);
        Encoding.UTF8.GetString(fila.Content).Should().Be("photo-bytes");
    }

    [Fact]
    public async Task NoDeberiaAbrirUnaFotoPublicaPorElCaminoPrivado()
    {
        await _storage.UploadAsync(Bytes("x"), "v.jpg", "image/jpeg", "vehicles");
        var key = (await _context.StoredFiles.SingleAsync()).StorageKey;

        var abierto = await _storage.OpenPrivateAsync(key);

        abierto.Should().BeNull("una clave pública no debe poder leerse como documento privado");
    }

    [Fact]
    public async Task DeberiaGuardarYRecuperarUnDocumentoPrivado()
    {
        var key = await _storage.UploadPrivateAsync(
            Bytes("carte-grise"), "cg.pdf", "application/pdf", "garage");

        key.Should().StartWith("garage/");

        await using var stream = await _storage.OpenPrivateAsync(key);
        stream.Should().NotBeNull();
        using var reader = new StreamReader(stream!);
        (await reader.ReadToEndAsync()).Should().Be("carte-grise");

        // Un documento privado no se sirve por la ruta pública: no debe existir esa fila.
        (await _context.StoredFiles.SingleAsync()).IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task DeberiaBorrarUnDocumentoPrivadoSinTocarLosDemas()
    {
        var a = await _storage.UploadPrivateAsync(Bytes("uno"), "a.pdf", "application/pdf", "garage");
        var b = await _storage.UploadPrivateAsync(Bytes("dos"), "b.pdf", "application/pdf", "garage");

        await _storage.DeletePrivateAsync(a);

        (await _storage.OpenPrivateAsync(a)).Should().BeNull();
        (await _storage.OpenPrivateAsync(b)).Should().NotBeNull();
        (await _context.StoredFiles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task VendreCeVehiculeDeberiaCopiarLaFotoSinVaciarElGaraje()
    {
        var key = await _storage.UploadPrivateAsync(
            Bytes("photo-privee"), "p.jpg", "image/jpeg", "garage");

        var publicado = await _storage.PublishPrivateAsync(key, "p.jpg", "image/jpeg", "vehicles");

        publicado.Should().NotBeNull();
        publicado!.Value.Url.Should().Contain("/files/vehicles/");

        // El original sigue en el garaje: se copió, no se movió.
        (await _storage.OpenPrivateAsync(key)).Should().NotBeNull();

        var filas = await _context.StoredFiles.ToListAsync();
        filas.Should().HaveCount(2);
        filas.Should().ContainSingle(f => f.IsPublic)
             .Which.Content.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("photo-privee"));
    }

    [Fact]
    public async Task PublicarUnaClaveInexistenteNoDeberiaCrearNada()
    {
        var r = await _storage.PublishPrivateAsync("garage/no-existe.jpg", "x.jpg", "image/jpeg", "vehicles");

        r.Should().BeNull();
        (await _context.StoredFiles.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task DeberiaBorrarUnaFotoPublicaPorSuUrl()
    {
        var (url, _) = await _storage.UploadAsync(Bytes("x"), "v.jpg", "image/jpeg", "vehicles");

        await _storage.DeleteAsync(url);

        (await _context.StoredFiles.AnyAsync()).Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
