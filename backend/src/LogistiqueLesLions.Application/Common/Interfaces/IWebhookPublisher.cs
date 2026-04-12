namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Publica eventos a un endpoint externo (n8n, Zapier, Make, Slack incoming webhook…).
/// Fire-and-forget desde los handlers: nunca debe romper el flujo de negocio si el
/// webhook está caído.
/// </summary>
public interface IWebhookPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default);
}
