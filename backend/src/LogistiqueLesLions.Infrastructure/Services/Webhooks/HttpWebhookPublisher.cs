using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiqueLesLions.Infrastructure.Services.Webhooks;

/// <summary>
/// Publica eventos vía HTTP POST a un endpoint configurable. Compatible con n8n
/// (webhook trigger), Zapier, Make o cualquier receptor que acepte JSON.
///
/// Si Webhooks:Secret está configurado, firma el cuerpo con HMAC-SHA256 y lo envía
/// en el header X-LLL-Signature para que n8n pueda verificar autenticidad.
/// </summary>
public class HttpWebhookPublisher(
    HttpClient httpClient,
    IOptions<WebhookOptions> options,
    ILogger<HttpWebhookPublisher> logger) : IWebhookPublisher
{
    private readonly WebhookOptions _options = options.Value;

    public async Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Url))
            return;

        var envelope = new
        {
            @event    = eventName,
            timestamp = DateTimeOffset.UtcNow,
            data      = payload
        };

        try
        {
            var body = JsonSerializer.Serialize(envelope);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(_options.Secret))
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                content.Headers.Add("X-LLL-Signature", Convert.ToHexString(hash).ToLowerInvariant());
            }
            content.Headers.Add("X-LLL-Event", eventName);

            var response = await httpClient.PostAsync(_options.Url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Webhook {Event} devolvió {Status}: {Body}",
                    eventName, response.StatusCode, responseBody);
            }
            else
            {
                logger.LogInformation("Webhook {Event} publicado correctamente", eventName);
            }
        }
        catch (Exception ex)
        {
            // Nunca tirar abajo el flujo de negocio por un webhook caído.
            logger.LogError(ex, "Error publicando webhook {Event}", eventName);
        }
    }
}
