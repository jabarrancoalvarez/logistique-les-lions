using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Communications;

/// <summary>
/// «Notifications et communications»: avisos de plataforma.
/// </summary>
/// <remarks>
/// Deliberadamente corto, como pide la especificación: avisos, mantenimiento,
/// información importante y soporte individual. No es una herramienta de marketing.
/// </remarks>
public record SendCommunicationCommand(
    Guid AdminId,
    CommunicationType Type,
    CommunicationAudience Audience,
    Guid? TargetUserId,
    string? Region,
    string Title,
    string Body,
    bool SendByEmail
) : IRequest<Result<SendCommunicationResultDto>>;

/// <param name="EmailsSent">
/// Menor que el total cuando hay destinatarios sin correo: el correo es opcional en
/// Yoon u Auto, donde la cuenta se identifica por teléfono.
/// </param>
public record SendCommunicationResultDto(Guid Id, int RecipientCount, int EmailsSent);

public class SendCommunicationCommandHandler(
    IApplicationDbContext db,
    IEmailSender? emailSender = null,
    INotificationPusher? pusher = null)
    : IRequestHandler<SendCommunicationCommand, Result<SendCommunicationResultDto>>
{
    public async Task<Result<SendCommunicationResultDto>> Handle(
        SendCommunicationCommand request, CancellationToken ct)
    {
        var title = request.Title?.Trim();
        var body = request.Body?.Trim();

        if (string.IsNullOrEmpty(title)) return Fail("Communication.TitleRequired");
        if (string.IsNullOrEmpty(body)) return Fail("Communication.BodyRequired");

        var recipients = await ResolveRecipientsAsync(request, ct);

        if (recipients is null) return Fail("Communication.TargetRequired");
        if (recipients.Count == 0) return Fail("Communication.NoRecipients");

        var communication = new Communication
        {
            AdminId      = request.AdminId,
            Type         = request.Type,
            Audience     = request.Audience,
            TargetUserId = request.Audience == CommunicationAudience.Individuel
                ? request.TargetUserId
                : null,
            Region       = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
            Title        = title,
            Body         = body,
            SentByEmail  = request.SendByEmail,
            RecipientCount = recipients.Count
        };

        var notifications = recipients
            .Select(r => new UserNotification
            {
                UserId   = r.Id,
                Category = NotificationCategories.System,
                Title    = title,
                Body     = body,
                Link     = "/ajustes"
            })
            .ToList();

        foreach (var notification in notifications) db.UserNotifications.Add(notification);

        // El correo solo llega a quien lo tiene: en Yoon u Auto es opcional.
        var withEmail = request.SendByEmail
            ? recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList()
            : [];

        communication.EmailsSent = withEmail.Count;

        db.Communications.Add(communication);
        await db.SaveChangesAsync(ct);

        // Fuera de la transacción: que un correo falle no puede deshacer el aviso ya
        // guardado, que es el canal principal.
        await pusher.PushAsync(notifications, ct);

        if (emailSender is not null)
        {
            foreach (var recipient in withEmail)
            {
                await emailSender.SendAsync(new EmailMessage(
                    To: recipient.Email!,
                    Subject: title,
                    HtmlBody: $"<p>{System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br>")}</p>",
                    ToName: recipient.DisplayName), ct);
            }
        }

        return Result<SendCommunicationResultDto>.Success(new SendCommunicationResultDto(
            communication.Id, communication.RecipientCount, communication.EmailsSent));
    }

    private sealed record Recipient(Guid Id, string DisplayName, string? Email);

    /// <summary><c>null</c> si la audiencia no se puede resolver.</summary>
    private async Task<List<Recipient>?> ResolveRecipientsAsync(
        SendCommunicationCommand request, CancellationToken ct)
    {
        if (request.Audience == CommunicationAudience.Individuel)
        {
            if (request.TargetUserId is not { } targetId) return null;

            return await db.UserProfiles
                .AsNoTracking()
                .Where(u => u.Id == targetId)
                .Select(u => new Recipient(u.Id, u.DisplayName, u.Email))
                .ToListAsync(ct);
        }

        // Las cuentas bloqueadas quedan fuera: un aviso de plataforma no es para quien
        // ya no puede entrar.
        var query = db.UserProfiles
            .AsNoTracking()
            .Where(u => u.Status != AccountStatus.Blocked);

        query = request.Audience switch
        {
            CommunicationAudience.Particuliers =>
                query.Where(u => u.AccountType == AccountType.Particulier),
            CommunicationAudience.Professionnels =>
                query.Where(u => u.AccountType == AccountType.Professionnel),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            query = query.Where(u => u.Region == region);
        }

        return await query
            .Select(u => new Recipient(u.Id, u.DisplayName, u.Email))
            .ToListAsync(ct);
    }

    private static Result<SendCommunicationResultDto> Fail(string error) =>
        Result<SendCommunicationResultDto>.Failure(error);
}

/// <summary>Histórico: qué se envió, cuándo, por quién y a cuántos.</summary>
public record GetCommunicationsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<CommunicationListDto>>;

public record CommunicationListDto(
    int TotalCount, int Page, int PageSize, IReadOnlyList<CommunicationRowDto> Items);

public record CommunicationRowDto(
    Guid Id,
    CommunicationType Type,
    CommunicationAudience Audience,
    string? TargetUserName,
    string? Region,
    string Title,
    string Body,
    bool SentByEmail,
    int RecipientCount,
    int EmailsSent,
    string AdminName,
    DateTimeOffset SentAt
);

public class GetCommunicationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCommunicationsQuery, Result<CommunicationListDto>>
{
    public async Task<Result<CommunicationListDto>> Handle(
        GetCommunicationsQuery request, CancellationToken ct)
    {
        var query = db.Communications.AsNoTracking();

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await query
            .Include(c => c.Admin)
            .Include(c => c.TargetUser)
            .OrderByDescending(c => c.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommunicationRowDto(
                c.Id, c.Type, c.Audience,
                c.TargetUser != null ? c.TargetUser.DisplayName : null,
                c.Region, c.Title, c.Body, c.SentByEmail,
                c.RecipientCount, c.EmailsSent,
                c.Admin != null ? c.Admin.DisplayName : "—",
                c.SentAt))
            .ToListAsync(ct);

        return Result<CommunicationListDto>.Success(
            new CommunicationListDto(total, page, pageSize, rows));
    }
}
