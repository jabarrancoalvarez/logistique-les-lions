using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiqueLesLions.Infrastructure.Services.Email;

/// <summary>
/// Cliente HTTP minimal para Resend (https://resend.com/docs/api-reference/emails/send-email).
/// No usamos el SDK oficial para evitar añadir un paquete más; el endpoint es estable.
/// </summary>
public class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Resend.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Resend API key no configurada — email a {To} no se ha enviado", message.To);
            return;
        }

        httpClient.BaseAddress ??= new Uri(_options.Resend.BaseUrl);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromAddress
            : $"{_options.FromName} <{_options.FromAddress}>";

        var to = string.IsNullOrWhiteSpace(message.ToName)
            ? message.To
            : $"{message.ToName} <{message.To}>";

        var payload = new ResendPayload
        {
            From = from,
            To = [to],
            Subject = message.Subject,
            Html = message.HtmlBody,
            ReplyTo = message.ReplyTo
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync("/emails", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Resend devolvió {Status} al enviar email a {To}: {Body}",
                    response.StatusCode, message.To, body);
                return;
            }

            logger.LogInformation("Email enviado vía Resend a {To}: {Subject}", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            // Nunca tirar abajo el flujo de negocio por un email caído.
            logger.LogError(ex, "Error inesperado enviando email a {To}", message.To);
        }
    }

    private sealed class ResendPayload
    {
        [JsonPropertyName("from")]     public string From { get; set; } = string.Empty;
        [JsonPropertyName("to")]       public string[] To { get; set; } = [];
        [JsonPropertyName("subject")]  public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("html")]     public string Html { get; set; } = string.Empty;
        [JsonPropertyName("reply_to")] public string? ReplyTo { get; set; }
    }
}
