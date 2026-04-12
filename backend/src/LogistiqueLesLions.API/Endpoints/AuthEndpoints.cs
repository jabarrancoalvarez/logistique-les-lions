using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Auth.Commands.Login;
using LogistiqueLesLions.Application.Features.Auth.Commands.RefreshToken;
using LogistiqueLesLions.Application.Features.Auth.Commands.Register;
using LogistiqueLesLions.Application.Features.Auth.Commands.UpdateProfile;
using LogistiqueLesLions.Application.Features.Auth.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/v1/auth/register
        group.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Created("/api/v1/auth/me", result) : Results.BadRequest(result);
        })
        .RequireRateLimiting("AuthRateLimit")
        .AllowAnonymous()
        .WithSummary("Registrar nuevo usuario");

        // POST /api/v1/auth/login
        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Unauthorized();
        })
        .RequireRateLimiting("AuthRateLimit")
        .AllowAnonymous()
        .WithSummary("Iniciar sesión");

        // POST /api/v1/auth/refresh
        group.MapPost("/refresh", async ([FromBody] RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Unauthorized();
        })
        .AllowAnonymous()
        .WithSummary("Renovar token de acceso");

        // GET /api/v1/auth/me
        group.MapGet("/me", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(new GetProfileQuery(currentUser.UserId.Value), ct);
            return result.IsSuccess ? Results.Ok(result) : Results.NotFound(result);
        })
        .RequireAuthorization()
        .WithSummary("Obtener perfil del usuario autenticado");

        // PUT /api/v1/auth/me
        group.MapPut("/me", async (
            [FromBody] UpdateProfileRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var command = new UpdateProfileCommand(
                currentUser.UserId.Value,
                req.FirstName, req.LastName, req.Phone,
                req.CountryCode, req.City, req.CompanyName,
                req.CompanyVat, req.Bio, req.AvatarUrl);

            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result);
        })
        .RequireAuthorization()
        .WithSummary("Actualizar perfil del usuario autenticado");

        // POST /api/v1/auth/logout  (invalida el refresh token)
        group.MapPost("/logout", async (ICurrentUser currentUser, IApplicationDbContext db, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var user = await db.UserProfiles.FindAsync([currentUser.UserId.Value], ct);
            if (user is not null)
            {
                user.RefreshToken          = null;
                user.RefreshTokenExpiresAt = null;
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithSummary("Cerrar sesión (invalida refresh token)");

        return group;
    }
}

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? CountryCode,
    string? City,
    string? CompanyName,
    string? CompanyVat,
    string? Bio,
    string? AvatarUrl
);
