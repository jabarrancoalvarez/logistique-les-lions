using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.PublicTracking.Queries.GetTrackingRoute;

/// <summary>
/// Devuelve coordenadas geográficas del recorrido para pintar un mapa
/// (Leaflet) en la página de tracking público. Sin datos personales.
/// </summary>
public record GetTrackingRouteQuery(string Code) : IRequest<Result<TrackingRouteDto>>;

public record GeoPoint(double Lat, double Lng, string Label);

public record TrackingRouteDto(
    string TrackingCode,
    GeoPoint Origin,
    GeoPoint Destination,
    int CompletionPercent,
    GeoPoint CurrentEstimatedPosition,
    string Status);

public class GetTrackingRouteQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTrackingRouteQuery, Result<TrackingRouteDto>>
{
    // Centroides aproximados para los países habituales del MVP.
    // Si el código no está en el diccionario se cae a (0,0) — no rompe.
    private static readonly Dictionary<string, (double Lat, double Lng, string Name)> Centroids = new()
    {
        ["ES"] = (40.4168, -3.7038,  "España"),
        ["FR"] = (46.6034,  1.8883,  "Francia"),
        ["DE"] = (51.1657, 10.4515,  "Alemania"),
        ["IT"] = (41.8719, 12.5674,  "Italia"),
        ["PT"] = (39.3999, -8.2245,  "Portugal"),
        ["NL"] = (52.1326,  5.2913,  "Países Bajos"),
        ["BE"] = (50.5039,  4.4699,  "Bélgica"),
        ["GB"] = (55.3781, -3.4360,  "Reino Unido"),
        ["JP"] = (36.2048, 138.2529, "Japón"),
        ["US"] = (37.0902,-95.7129,  "EE.UU."),
        ["MA"] = (31.7917, -7.0926,  "Marruecos"),
        ["CH"] = (46.8182,  8.2275,  "Suiza"),
    };

    public async Task<Result<TrackingRouteDto>> Handle(GetTrackingRouteQuery request, CancellationToken ct)
    {
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length is < 6 or > 16)
            return Result<TrackingRouteDto>.Failure("Tracking.InvalidCode");

        var p = await db.ImportExportProcesses.AsNoTracking()
            .Where(x => x.TrackingCode == code)
            .Select(x => new
            {
                x.TrackingCode, x.OriginCountry, x.DestinationCountry,
                x.CompletionPercent, x.Status
            })
            .FirstOrDefaultAsync(ct);

        if (p is null)
            return Result<TrackingRouteDto>.Failure("Tracking.NotFound");

        var origin = ToGeo(p.OriginCountry);
        var dest   = ToGeo(p.DestinationCountry);

        // Posición estimada: interpolación lineal según el % de avance.
        var t = Math.Clamp(p.CompletionPercent / 100d, 0d, 1d);
        var current = new GeoPoint(
            Lat:   origin.Lat + (dest.Lat - origin.Lat) * t,
            Lng:   origin.Lng + (dest.Lng - origin.Lng) * t,
            Label: $"En tránsito ({p.CompletionPercent}%)");

        return Result<TrackingRouteDto>.Success(new TrackingRouteDto(
            p.TrackingCode, origin, dest, p.CompletionPercent, current, p.Status.ToString()));
    }

    private static GeoPoint ToGeo(string isoCode)
    {
        var key = (isoCode ?? string.Empty).ToUpperInvariant();
        if (Centroids.TryGetValue(key, out var c))
            return new GeoPoint(c.Lat, c.Lng, c.Name);
        return new GeoPoint(0, 0, key);
    }
}
