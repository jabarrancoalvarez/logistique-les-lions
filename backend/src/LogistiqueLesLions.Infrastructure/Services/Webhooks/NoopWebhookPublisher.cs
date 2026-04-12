using LogistiqueLesLions.Application.Common.Interfaces;

namespace LogistiqueLesLions.Infrastructure.Services.Webhooks;

/// <summary>Fallback registrado cuando Webhooks:Enabled es false.</summary>
public class NoopWebhookPublisher : IWebhookPublisher
{
    public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
