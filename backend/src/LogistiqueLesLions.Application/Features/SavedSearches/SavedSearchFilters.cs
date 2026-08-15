using System.Text.Json;
using System.Text.Json.Serialization;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

namespace LogistiqueLesLions.Application.Features.SavedSearches;

/// <summary>
/// Serialización de los filtros de una búsqueda guardada.
/// </summary>
/// <remarks>
/// Vive aquí y no repartida por los handlers para que guardar y leer usen exactamente
/// las mismas opciones: si divergieran, una búsqueda guardada devolvería resultados
/// distintos de los que el usuario vio al crearla.
/// </remarks>
public static class SavedSearchFilters
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Los enums se guardan por nombre: un JSON con "Dedouane" sobrevive a que
        // mañana se reordene el enum; uno con "1" no.
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Campos que no forman parte de la búsqueda del usuario.</summary>
    public static GetVehiclesQuery Sanitize(GetVehiclesQuery filters) => filters with
    {
        // La paginación y la ordenación son estado de la pantalla, no criterios.
        Page = 1,
        PageSize = 20,
        // Nunca se guarda permiso para ver anuncios no públicos ni filtros internos.
        IncludeNonPublic = false,
        SellerId = null,
        Status = null,
        IsFeatured = null
    };

    public static string Serialize(GetVehiclesQuery filters) =>
        JsonSerializer.Serialize(Sanitize(filters), Options);

    /// <summary>
    /// Deserializa los filtros guardados. Devuelve una búsqueda vacía si el JSON es
    /// ilegible, para que una fila corrupta no tumbe el listado entero.
    /// </summary>
    public static GetVehiclesQuery Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<GetVehiclesQuery>(json, Options) ?? new GetVehiclesQuery();
        }
        catch (JsonException)
        {
            return new GetVehiclesQuery();
        }
    }
}
