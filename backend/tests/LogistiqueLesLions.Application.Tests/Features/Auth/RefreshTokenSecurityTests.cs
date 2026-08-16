using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Auth;
using LogistiqueLesLions.Application.Features.Auth.Commands.RefreshToken;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Auth;

/// <summary>
/// Renovación de la sesión.
/// </summary>
/// <remarks>
/// Estas pruebas existen por un fallo de autenticación encontrado en producción:
/// enviar <c>{"refreshToken": null}</c> <b>sin credencial ninguna</b> devolvía un token
/// de acceso de administrador.
///
/// Dos descuidos se sumaban. La consulta buscaba por igualdad sin validar la entrada, de
/// modo que un nulo casaba con la primera cuenta que tuviera la columna a nulo —la de
/// administración sembrada, que nunca había iniciado sesión—. Y el guardián de caducidad
/// preguntaba si la fecha ya había pasado, cosa que es <b>falsa</b> cuando la fecha es
/// nula, porque en C# toda comparación con un nulo levantado devuelve falso.
/// </remarks>
public class RefreshTokenSecurityTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RefreshTokenCommandHandler _handler;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RefreshTokenSecurityTests()
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
                currentUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        _context.UserProfiles.AddRange(
            // La cuenta del ataque: administrador que nunca inició sesión, así que su
            // refresh token y su caducidad están a nulo.
            new UserProfile
            {
                Id = _adminId, DisplayName = "Administration", Phone = "+221770000001",
                PasswordHash = "x", Role = UserRole.Admin,
            },
            new UserProfile
            {
                Id = _userId, DisplayName = "Mamadou Diop", Phone = "+221770000002",
                PasswordHash = "x",
            });

        // Las sesiones viven en su propia tabla: una fila por dispositivo. La cuenta del
        // ataque no tiene ninguna, que es justo el caso que el fallo antiguo explotaba.
        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = _userId,
            Token     = "un-token-valide",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(20)
        });
        _context.SaveChanges();

        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GenerateRefreshToken()).Returns("nouveau-refresh");
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<UserProfile>())).Returns("nouvel-access");

        _handler = new RefreshTokenCommandHandler(_context, jwt.Object);
    }

    private Task<LogistiqueLesLions.Application.Common.Models.Result<AuthResponseDto>> RefreshAsync(
        string? token) =>
        _handler.Handle(new RefreshTokenCommand(token!), CancellationToken.None);

    // ─── El fallo ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnTokenNuloNoDebeAutenticarANadie()
    {
        // Esto devolvía un token de administrador a cualquiera que lo pidiera.
        var result = await RefreshAsync(null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task UnTokenVacioTampoco()
    {
        (await RefreshAsync("")).IsSuccess.Should().BeFalse();
        (await RefreshAsync("   ")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UnaSesionCaducadaNoDebePoderRenovar()
    {
        // Aunque se acierte el token, una sesión vencida no renueva. Se pregunta en
        // positivo —que la caducidad exista y esté en el futuro— porque el guardián
        // anterior comparaba «null < ahora», que en C# siempre es falso.
        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = _adminId,
            Token     = "token-perime",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        (await RefreshAsync("token-perime")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UnaSesionRevocadaNoDebePoderRenovar()
    {
        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = _adminId,
            Token     = "token-revoque",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(20),
            RevokedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();

        (await RefreshAsync("token-revoque")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CerrarUnaSesionNoDebeCerrarLasDemas()
    {
        // Entrar desde el móvil no puede expulsar al ordenador.
        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = _userId,
            Token     = "session-du-mobile",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(20)
        });
        await _context.SaveChangesAsync();

        // Se usa —y por tanto se rota— la del ordenador.
        (await RefreshAsync("un-token-valide")).IsSuccess.Should().BeTrue();

        // La del móvil sigue valiendo.
        (await RefreshAsync("session-du-mobile")).IsSuccess.Should().BeTrue();
    }

    // ─── Lo que sí debe funcionar ──────────────────────────────────────────

    [Fact]
    public async Task UnTokenValidoDebeRenovarLaSesion()
    {
        var result = await RefreshAsync("un-token-valide");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("nouvel-access");
        result.Value.RefreshToken.Should().Be("nouveau-refresh");
    }

    [Fact]
    public async Task UsarElTokenDosVecesNoDebeFuncionar()
    {
        // El servidor rota el token en cada uso: el anterior deja de valer.
        await RefreshAsync("un-token-valide");

        (await RefreshAsync("un-token-valide")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UnTokenCaducadoNoDebeRenovar()
    {
        var sesion = await _context.UserRefreshTokens
            .FirstAsync(t => t.Token == "un-token-valide");
        sesion.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        (await RefreshAsync("un-token-valide")).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UnaCuentaBloqueadaNoDebeRenovarSuSesion()
    {
        // Si no, seguiría dentro hasta que caducara el refresh token.
        var user = await _context.UserProfiles.FirstAsync(u => u.Id == _userId);
        user.Status = AccountStatus.Blocked;
        await _context.SaveChangesAsync();

        (await RefreshAsync("un-token-valide")).IsSuccess.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
