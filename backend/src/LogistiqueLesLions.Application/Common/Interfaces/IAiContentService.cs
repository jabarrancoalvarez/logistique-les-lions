namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Servicios de IA (Anthropic Claude): extrae datos de documentos y responde preguntas
/// sobre un vehículo.
/// </summary>
/// <remarks>
/// ❌ Ya no genera descripciones de anuncios: el documento funcional lo prohíbe.
/// </remarks>
public interface IAiContentService
{
    Task<AiDocumentExtraction> ExtractVehicleDocumentAsync(
        byte[] imageBytes, string mediaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat contextual: el LLM responde en base al vehículo + historial de mensajes previos.
    /// </summary>
    Task<AiChatReply> AnswerVehicleQuestionAsync(
        VehicleAiContext context,
        IReadOnlyList<AiChatTurn> history,
        string question,
        CancellationToken cancellationToken = default);
}

public record AiChatTurn(string Role, string Content); // Role: "user" | "assistant"
public record AiChatReply(string Answer);

public record VehicleAiContext(
    string Make,
    string? Model,
    int Year,
    int? Mileage,
    string? FuelType,
    string? Transmission,
    string? BodyType,
    string? Color,
    string Condition,
    decimal Price,
    string Currency,
    string CountryOrigin,
    bool IsExportReady);

public record AiDocumentExtraction(
    string? Vin,
    string? Make,
    string? Model,
    int? Year,
    string? LicensePlate,
    string? Color,
    int? Mileage,
    string? FuelType,
    string? RawJson);
