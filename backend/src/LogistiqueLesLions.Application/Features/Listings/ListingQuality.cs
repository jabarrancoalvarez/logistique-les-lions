using LogistiqueLesLions.Domain.Entities;

namespace LogistiqueLesLions.Application.Features.Listings;

/// <summary>Apartados que suman calidad al anuncio. El frontend les pone nombre en francés.</summary>
public enum ListingQualityCheck
{
    /// <summary>Photographies.</summary>
    Photos = 1,
    /// <summary>Description.</summary>
    Description = 2,
    /// <summary>Prix renseigné.</summary>
    Price = 3,
    /// <summary>Kilométrage.</summary>
    Mileage = 4,
    /// <summary>Localisation.</summary>
    Location = 6,
    /// <summary>Fiche technique.</summary>
    Specifications = 7,
    /// <summary>Équipements.</summary>
    Equipment = 8
}

public enum ListingQualityStatus { Missing = 0, Partial = 1, Complete = 2 }

public record ListingQualityItemDto(
    ListingQualityCheck Check,
    ListingQualityStatus Status,
    int Points,
    int MaxPoints,
    int? Detail
);

public record ListingQualityDto(int Score, IReadOnlyList<ListingQualityItemDto> Items);

/// <summary>
/// «Qualité de l'annonce»: lo completo que está el anuncio de cara a quien lo mira.
/// </summary>
/// <remarks>
/// No dice nada del vehículo —para eso está la complétude de Mon Garage— sino de lo bien
/// presentado que está: un anuncio con una sola foto y sin descripción se ve menos y se
/// pregunta más. Reglas, sin IA.
/// </remarks>
public static class ListingQualityCalculator
{
    // Los pesos suman 100 y están juntos a la vista, como en la complétude del garaje.
    private const int PhotosPoints         = 30;
    private const int DescriptionPoints    = 25;
    private const int PricePoints          = 15;
    private const int MileagePoints        = 10;
    private const int LocationPoints       = 10;
    private const int SpecificationsPoints = 5;
    private const int EquipmentPoints      = 5;

    /// <summary>Fotografías a partir de las cuales el anuncio se considera bien ilustrado.</summary>
    private const int PhotoTarget = 5;

    /// <summary>Caracteres a partir de los cuales la descripción dice algo.</summary>
    private const int DescriptionTarget = 200;

    /// <summary>Equipamientos a partir de los cuales la lista resulta útil.</summary>
    private const int EquipmentTarget = 5;

    public static ListingQualityDto For(Vehicle vehicle)
    {
        var items = new List<ListingQualityItemDto>
        {
            Item(ListingQualityCheck.Photos,
                Math.Min(vehicle.Images.Count, PhotoTarget), PhotoTarget, PhotosPoints,
                vehicle.Images.Count),

            Item(ListingQualityCheck.Description,
                Math.Min(vehicle.Description?.Trim().Length ?? 0, DescriptionTarget),
                DescriptionTarget, DescriptionPoints,
                vehicle.Description?.Trim().Length ?? 0),

            // Un anuncio sin precio no puede compararse ni filtrarse: es lo que más
            // frena a quien busca.
            Item(ListingQualityCheck.Price, vehicle.Price > 0 ? 1 : 0, 1, PricePoints),

            Item(ListingQualityCheck.Mileage, vehicle.Mileage is > 0 ? 1 : 0, 1, MileagePoints),

            Item(ListingQualityCheck.Location,
                new[] { !string.IsNullOrWhiteSpace(vehicle.Region),
                        !string.IsNullOrWhiteSpace(vehicle.City) }.Count(x => x),
                2, LocationPoints),

            Item(ListingQualityCheck.Specifications,
                new[] { vehicle.FuelType is not null,
                        vehicle.Transmission is not null,
                        vehicle.BodyType is not null,
                        vehicle.PowerCv is not null }.Count(x => x),
                4, SpecificationsPoints),

            Item(ListingQualityCheck.Equipment,
                Math.Min(vehicle.Equipments.Count, EquipmentTarget), EquipmentTarget,
                EquipmentPoints, vehicle.Equipments.Count)
        };

        return new ListingQualityDto(items.Sum(i => i.Points), items);
    }

    private static ListingQualityItemDto Item(
        ListingQualityCheck check, int achieved, int total, int maxPoints, int? detail = null)
    {
        var points = total == 0 ? 0 : (int)Math.Round((decimal)achieved / total * maxPoints);

        var status = achieved == 0 ? ListingQualityStatus.Missing
                   : achieved >= total ? ListingQualityStatus.Complete
                   : ListingQualityStatus.Partial;

        return new ListingQualityItemDto(check, status, points, maxPoints, detail);
    }
}
