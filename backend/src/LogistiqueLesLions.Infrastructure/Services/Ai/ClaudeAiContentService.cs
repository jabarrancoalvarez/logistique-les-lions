using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiqueLesLions.Infrastructure.Services.Ai;

/// <summary>
/// Cliente HTTP minimal para la Anthropic Messages API.
/// Sin SDK: el endpoint /v1/messages es estable y aporta menos superficie de fallo.
///
/// Para descripciones: pedimos JSON estricto con dos campos (es, en).
/// Para OCR: enviamos la imagen en base64 + prompt que pide JSON con los campos del vehículo.
/// </summary>
public class ClaudeAiContentService(
    HttpClient httpClient,
    IOptions<AnthropicOptions> options,
    ILogger<ClaudeAiContentService> logger) : IAiContentService
{
    private readonly AnthropicOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AiVehicleDescription> GenerateVehicleDescriptionAsync(
        VehicleAiContext c, CancellationToken cancellationToken = default)
    {
        var prompt =
            $$"""
            Genera una descripción comercial para este vehículo en dos idiomas. Tono profesional,
            destaca puntos fuertes (eficiencia, equipamiento, estado), 80-120 palabras por idioma.
            NO inventes datos que no aparezcan abajo. NO incluyas precio ni emojis.

            Datos:
            - Marca/Modelo: {{c.Make}} {{c.Model}}
            - Año: {{c.Year}}
            - Kilometraje: {{(c.Mileage?.ToString() ?? "no especificado")}}
            - Combustible: {{c.FuelType ?? "no especificado"}}
            - Transmisión: {{c.Transmission ?? "no especificada"}}
            - Carrocería: {{c.BodyType ?? "no especificada"}}
            - Color: {{c.Color ?? "no especificado"}}
            - Estado: {{c.Condition}}
            - País de origen: {{c.CountryOrigin}}
            - Listo para exportación: {{(c.IsExportReady ? "sí" : "no")}}

            Devuelve ÚNICAMENTE un objeto JSON con esta forma exacta, sin markdown ni texto extra:
            {"es": "descripción en español", "en": "description in english"}
            """;

        var json = await SendMessageAsync(
            new[] { new ContentBlock { Type = "text", Text = prompt } },
            cancellationToken);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var es = doc.RootElement.GetProperty("es").GetString() ?? string.Empty;
            var en = doc.RootElement.GetProperty("en").GetString() ?? string.Empty;
            return new AiVehicleDescription(es, en);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Respuesta de Claude no parseable como JSON: {Json}", json);
            return new AiVehicleDescription(json, string.Empty);
        }
    }

    public async Task<AiDocumentExtraction> ExtractVehicleDocumentAsync(
        byte[] imageBytes, string mediaType, CancellationToken cancellationToken = default)
    {
        var base64 = Convert.ToBase64String(imageBytes);

        var prompt =
            """
            Esta imagen es un documento vehicular (ficha técnica, COC, permiso de circulación o similar).
            Extrae los siguientes campos en JSON. Si un campo no es visible, devuelve null.
            NO inventes valores. NO añadas markdown ni texto fuera del JSON.

            Forma exacta:
            {
              "vin": "...",
              "make": "...",
              "model": "...",
              "year": 2020,
              "licensePlate": "...",
              "color": "...",
              "mileage": 50000,
              "fuelType": "Diesel|Gasolina|Electric|Hybrid|..."
            }
            """;

        var json = await SendMessageAsync(new[]
        {
            new ContentBlock
            {
                Type = "image",
                Source = new ImageSource { Type = "base64", MediaType = mediaType, Data = base64 }
            },
            new ContentBlock { Type = "text", Text = prompt }
        }, cancellationToken);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new AiDocumentExtraction(
                Vin:          GetStr(root, "vin"),
                Make:         GetStr(root, "make"),
                Model:        GetStr(root, "model"),
                Year:         GetInt(root, "year"),
                LicensePlate: GetStr(root, "licensePlate"),
                Color:        GetStr(root, "color"),
                Mileage:      GetInt(root, "mileage"),
                FuelType:     GetStr(root, "fuelType"),
                RawJson:      json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Respuesta OCR de Claude no parseable: {Json}", json);
            return new AiDocumentExtraction(null, null, null, null, null, null, null, null, json);
        }
    }

    public async Task<AiChatReply> AnswerVehicleQuestionAsync(
        VehicleAiContext c,
        IReadOnlyList<AiChatTurn> history,
        string question,
        CancellationToken cancellationToken = default)
    {
        var systemContext =
            $$"""
            Eres el asistente comercial de Logistique Les Lions, especializado en compraventa
            internacional de vehículos. Respondes en el mismo idioma que el usuario, en tono
            profesional y conciso. NO inventes datos que no aparezcan en la ficha del vehículo.
            Si no sabes algo, dilo claramente y propón contactar con un asesor humano.

            Ficha del vehículo:
            - {{c.Make}} {{c.Model}} ({{c.Year}})
            - Kilometraje: {{(c.Mileage?.ToString() ?? "no especificado")}}
            - Combustible: {{c.FuelType ?? "no especificado"}}
            - Transmisión: {{c.Transmission ?? "no especificada"}}
            - Carrocería: {{c.BodyType ?? "no especificada"}}
            - Color: {{c.Color ?? "no especificado"}}
            - Estado: {{c.Condition}}
            - Precio: {{c.Price}} {{c.Currency}}
            - País origen: {{c.CountryOrigin}}
            - Listo para exportación: {{(c.IsExportReady ? "sí" : "no")}}
            """;

        var messages = new List<Message>(history.Count + 1);
        foreach (var turn in history)
        {
            var role = turn.Role == "assistant" ? "assistant" : "user";
            messages.Add(new Message
            {
                Role = role,
                Content = [new ContentBlock { Type = "text", Text = turn.Content }]
            });
        }
        messages.Add(new Message
        {
            Role = "user",
            Content = [new ContentBlock { Type = "text", Text = question }]
        });

        var answer = await SendMessageAsync(messages.ToArray(), systemContext, cancellationToken);
        return new AiChatReply(answer.Trim());
    }

    // ────────────────────────────────────────────────────────────────────────
    private Task<string> SendMessageAsync(ContentBlock[] content, CancellationToken ct)
        => SendMessageAsync(new[] { new Message { Role = "user", Content = content } }, null, ct);

    private async Task<string> SendMessageAsync(Message[] messages, string? system, CancellationToken ct)
    {
        httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
        httpClient.DefaultRequestHeaders.Remove("x-api-key");
        httpClient.DefaultRequestHeaders.Remove("anthropic-version");
        httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", _options.Version);

        var payload = new MessagesRequest
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = system,
            Messages = messages
        };

        var response = await httpClient.PostAsJsonAsync("/v1/messages", payload, JsonOpts, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Anthropic API devolvió {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Anthropic API error {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<MessagesResponse>(JsonOpts, ct)
                     ?? throw new InvalidOperationException("Respuesta vacía de Anthropic");

        // Concatenar todos los bloques de tipo text
        return string.Concat(result.Content?
            .Where(b => b.Type == "text")
            .Select(b => b.Text ?? string.Empty) ?? []);
    }

    private static string? GetStr(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var v) ? v : null;

    // ─── DTOs Anthropic ─────────────────────────────────────────────────────
    private sealed class MessagesRequest
    {
        [JsonPropertyName("model")]      public string Model { get; set; } = string.Empty;
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }
        [JsonPropertyName("system")]     public string? System { get; set; }
        [JsonPropertyName("messages")]   public Message[] Messages { get; set; } = [];
    }

    private sealed class Message
    {
        [JsonPropertyName("role")]    public string Role { get; set; } = "user";
        [JsonPropertyName("content")] public ContentBlock[] Content { get; set; } = [];
    }

    private sealed class ContentBlock
    {
        [JsonPropertyName("type")]   public string Type { get; set; } = "text";
        [JsonPropertyName("text")]   public string? Text { get; set; }
        [JsonPropertyName("source")] public ImageSource? Source { get; set; }
    }

    private sealed class ImageSource
    {
        [JsonPropertyName("type")]       public string Type { get; set; } = "base64";
        [JsonPropertyName("media_type")] public string MediaType { get; set; } = "image/jpeg";
        [JsonPropertyName("data")]       public string Data { get; set; } = string.Empty;
    }

    private sealed class MessagesResponse
    {
        [JsonPropertyName("content")] public ContentBlock[]? Content { get; set; }
    }
}
