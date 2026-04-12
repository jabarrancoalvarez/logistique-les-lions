namespace LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingByCode;

/// <summary>
/// Vista pública (sin login) del estado de un proceso de tramitación.
/// IMPORTANTE: NO incluye datos personales — solo lo que un destinatario
/// debe ver al introducir su código de tracking.
/// </summary>
public record PublicTrackingDto(
    string TrackingCode,
    string Status,
    int CompletionPercent,
    string OriginCountry,
    string DestinationCountry,
    string ProcessType,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset LastUpdatedAt,
    string VehicleTitle);
