namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Mensaje de email transaccional. Body en HTML; el implementador puede
/// derivar texto plano automáticamente si el proveedor lo necesita.
/// </summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? ToName = null,
    string? ReplyTo = null);

/// <summary>
/// Abstracción de envío de correo. Implementaciones disponibles:
///   - ConsoleEmailSender (default en dev / sin API key)
///   - ResendEmailSender  (cuando Email:Provider == "Resend")
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
