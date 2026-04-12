using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogistiqueLesLions.Infrastructure.Services.Email;

/// <summary>
/// Implementación de fallback que solo logea el email en consola.
/// Útil en desarrollo y como red de seguridad cuando no hay API key configurada,
/// para evitar que la aplicación se rompa silenciosamente.
/// </summary>
public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "📧 [DEV EMAIL] To: {To} | Subject: {Subject}\n{Body}",
            message.To, message.Subject, message.HtmlBody);
        return Task.CompletedTask;
    }
}
