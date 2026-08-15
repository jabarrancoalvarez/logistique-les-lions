using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Users;

/// <summary>
/// Activar, suspender temporalmente o bloquear una cuenta.
/// </summary>
/// <param name="SuspendedUntil">
/// Obligatorio al suspender: una suspensión sin final es un bloqueo con otro nombre.
/// </param>
public record ChangeAccountStatusCommand(
    Guid AdminId,
    Guid UserId,
    AccountStatus Status,
    string? Reason,
    DateTimeOffset? SuspendedUntil = null
) : IRequest<Result>;

public record AddAdminNoteCommand(
    Guid AdminId,
    AdminTargetType TargetType,
    Guid TargetId,
    string Body
) : IRequest<Result<Guid>>;

public record DeleteAdminNoteCommand(Guid AdminId, Guid NoteId) : IRequest<Result>;

// ─── Handlers ──────────────────────────────────────────────────────────────

public class ChangeAccountStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ChangeAccountStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeAccountStatusCommand request, CancellationToken ct)
    {
        // Un administrador no se toca a sí mismo: dejaría la plataforma sin quien la
        // gestione y no hay forma de deshacerlo desde dentro.
        if (request.AdminId == request.UserId)
            return Result.Failure("Admin.CannotActOnSelf");

        var user = await db.UserProfiles.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null) return Result.Failure("User.NotFound");

        // La gestión de administradores no se hace desde esta pantalla.
        if (user.Role == UserRole.Admin)
            return Result.Failure("Admin.CannotActOnAdmin");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        // Restringir una cuenta exige explicar por qué: es lo que se lee después en el
        // histórico cuando alguien pregunta qué pasó.
        if (request.Status != AccountStatus.Active && reason is null)
            return Result.Failure("Admin.ReasonRequired");

        if (request.Status == AccountStatus.Suspended)
        {
            if (request.SuspendedUntil is not { } until)
                return Result.Failure("Admin.SuspensionEndRequired");

            if (until <= DateTimeOffset.UtcNow)
                return Result.Failure("Admin.SuspensionEndInPast");

            user.SuspendedUntil = until;
        }
        else
        {
            user.SuspendedUntil = null;
        }

        user.Status = request.Status;

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.User,
            TargetId   = user.Id,
            Type = request.Status switch
            {
                AccountStatus.Suspended => AdminActionType.AccountSuspended,
                AccountStatus.Blocked   => AdminActionType.AccountBlocked,
                _                       => AdminActionType.AccountActivated
            },
            Reason = reason
        });

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class AddAdminNoteCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddAdminNoteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddAdminNoteCommand request, CancellationToken ct)
    {
        var body = request.Body.Trim();
        if (string.IsNullOrEmpty(body)) return Result<Guid>.Failure("Admin.NoteRequired");

        var exists = request.TargetType switch
        {
            AdminTargetType.User => await db.UserProfiles.AnyAsync(u => u.Id == request.TargetId, ct),
            AdminTargetType.Listing => await db.Vehicles.IgnoreQueryFilters()
                .AnyAsync(v => v.Id == request.TargetId && v.DeletedAt == null, ct),
            _ => false
        };

        if (!exists) return Result<Guid>.Failure("Admin.TargetNotFound");

        var note = new AdminNote
        {
            AdminId    = request.AdminId,
            TargetType = request.TargetType,
            TargetId   = request.TargetId,
            Body       = body
        };

        db.AdminNotes.Add(note);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(note.Id);
    }
}

public class DeleteAdminNoteCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteAdminNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteAdminNoteCommand request, CancellationToken ct)
    {
        var note = await db.AdminNotes.FirstOrDefaultAsync(n => n.Id == request.NoteId, ct);
        if (note is null) return Result.Failure("Admin.NoteNotFound");

        // Cada quien retira sus propias notas: son contexto de trabajo firmado.
        if (note.AdminId != request.AdminId) return Result.Failure("Admin.NotNoteAuthor");

        note.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
