using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Renueva el par de tokens a partir de un refresh token válido.
/// </summary>
/// <remarks>
/// ⚠️ Este handler tuvo un fallo de autenticación grave y las dos comprobaciones de
/// abajo son las que lo cierran. Conviene entender por qué antes de tocarlas.
///
/// La versión anterior buscaba el usuario por <c>u.RefreshToken == request.RefreshToken</c>
/// sin validar la entrada. Enviando <c>{"refreshToken": null}</c>, la consulta encontraba
/// al primer usuario con la columna a nulo —cualquiera que no hubiera iniciado sesión
/// nunca, como la cuenta de administración sembrada— y devolvía un token de acceso suyo
/// a quien lo pidiera, <b>sin credencial ninguna</b>.
///
/// El guardián de caducidad tampoco lo frenaba: <c>user.RefreshTokenExpiresAt &lt; UtcNow</c>
/// es <b>falso</b> cuando la fecha es nula, porque en C# toda comparación con un nulo
/// levantado devuelve falso. Un usuario sin fecha pasaba el filtro.
///
/// De ahí las dos reglas: <b>rechazar la entrada vacía antes de consultar</b> y exigir
/// que la caducidad <b>exista y esté en el futuro</b>, en vez de comprobar que no haya
/// pasado.
/// </remarks>
public class RefreshTokenCommandHandler(
    IApplicationDbContext db,
    IJwtService jwt)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        // Sin token no se busca nada: una cadena vacía o nula casaría con las cuentas
        // que tienen la columna a nulo.
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<AuthResponseDto>.Failure("Auth.InvalidRefreshToken");

        var token = request.RefreshToken;

        var sesion = await db.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        var user = sesion?.User;

        // La sesión tiene que existir, no estar revocada y no haber caducado. Se
        // pregunta en positivo a propósito: «que no haya pasado» dejaba entrar a quien
        // tuviera la fecha a nulo.
        if (sesion is null || user is null || !sesion.IsActive)
            return Result<AuthResponseDto>.Failure("Auth.InvalidRefreshToken");

        // Una cuenta bloqueada no renueva su sesión: si no, seguiría dentro hasta que
        // caducara el refresh token.
        if (!user.CanSignIn)
            return Result<AuthResponseDto>.Failure("Auth.InvalidRefreshToken");

        // Rotación: el token usado deja de valer y se emite otro para esta misma
        // sesión. Las demás sesiones de la cuenta no se tocan.
        var newRefresh = jwt.GenerateRefreshToken();
        sesion.RevokedAt = DateTimeOffset.UtcNow;

        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = user.Id,
            Token     = newRefresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            UserAgent = sesion.UserAgent
        });

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var access    = jwt.GenerateAccessToken(user);
        var expiresAt2 = DateTimeOffset.UtcNow.AddMinutes(15);

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(access, newRefresh, expiresAt2, UserDto.From(user)));
    }
}
