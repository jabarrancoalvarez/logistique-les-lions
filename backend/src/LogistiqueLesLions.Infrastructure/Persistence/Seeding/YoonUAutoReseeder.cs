using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogistiqueLesLions.Infrastructure.Persistence.Seeding;

/// <summary>
/// Sustituye el catálogo de demostración del producto anterior por uno senegalés.
/// </summary>
/// <remarks>
/// La base de producción arrastraba los veinte anuncios del seed original —BMW en
/// Múnich, Audi en Frankfurt— con precios en euros mostrados como FCFA. No es un fallo
/// de código, pero hace inservible todo lo que se apoya en comparables reales: el
/// indicador de precio, la estimación de valor y las estadísticas.
///
/// Se ejecuta <b>una sola vez</b>: la condición de arranque es que existan anuncios de
/// fuera de Senegal. Una vez retirados, no vuelve a hacer nada.
///
/// ❌ Nada se borra físicamente, como manda la regla del proyecto: los anuncios heredados
/// se marcan como eliminados y el filtro global del <see cref="ApplicationDbContext"/>
/// los esconde de todas las consultas —marketplace, backoffice y estadísticas— sin tocar
/// las claves ajenas de los procesos y conversaciones que colgaban de ellos.
/// </remarks>
public class YoonUAutoReseeder(
    ApplicationDbContext db,
    IConfiguration configuration,
    ILogger<YoonUAutoReseeder> logger)
{
    /// <summary>Teléfono de la cuenta que debe tener el backoffice. Nunca su contraseña.</summary>
    private string? adminPhone => configuration["Seed:AdminPhone"];

    /// <summary>Precio de referencia por modelo, en FCFA, para un ejemplar medio.</summary>
    private sealed record ModelSpec(
        string Make,
        string Model,
        BodyType Body,
        FuelType Fuel,
        TransmissionType Transmission,
        decimal BasePrice,
        int BaseYear,
        int BaseMileage);

    /// <summary>
    /// Los modelos que de verdad circulan en Senegal.
    /// </summary>
    /// <remarks>
    /// Cada uno recibe <b>seis o más ejemplares</b> a propósito: el indicador de precio
    /// exige cinco comparables antes de atreverse a decir nada, y con menos la pantalla
    /// no mostraría nada y no habría forma de probarlo.
    /// </remarks>
    private static readonly ModelSpec[] Catalogue =
    [
        new("Toyota",     "Hilux",     BodyType.PickUp,   FuelType.Diesel,  TransmissionType.Manuel,      14_500_000m, 2019, 120_000),
        new("Toyota",     "RAV4",      BodyType.Suv,      FuelType.Essence, TransmissionType.Automatique,  9_500_000m, 2019, 118_000),
        new("Toyota",     "Corolla",   BodyType.Berline,  FuelType.Essence, TransmissionType.Automatique,  6_800_000m, 2018, 135_000),
        new("Hyundai",    "Tucson",    BodyType.Suv,      FuelType.Diesel,  TransmissionType.Automatique,  8_900_000m, 2018, 128_000),
        new("Nissan",     "Qashqai",   BodyType.Suv,      FuelType.Diesel,  TransmissionType.Manuel,       7_400_000m, 2017, 145_000),
        new("Mercedes-Benz","Classe C",BodyType.Berline,  FuelType.Diesel,  TransmissionType.Automatique, 11_500_000m, 2017, 150_000),
        new("Peugeot",    "208",       BodyType.Citadine, FuelType.Essence, TransmissionType.Manuel,       4_200_000m, 2018, 110_000),
        new("Renault",    "Duster",    BodyType.Suv,      FuelType.Diesel,  TransmissionType.Manuel,       6_100_000m, 2019,  95_000)
    ];

    /// <summary>Ciudades reales, con su región, repartidas por el país.</summary>
    private static readonly (string Region, string City)[] Places =
    [
        ("DK", "Dakar"), ("DK", "Rufisque"), ("DK", "Pikine"), ("DK", "Guédiawaye"),
        ("TH", "Thiès"), ("TH", "Mbour"), ("TH", "Tivaouane"),
        ("SL", "Saint-Louis"), ("DB", "Touba"), ("DB", "Diourbel"),
        ("KL", "Kaolack"), ("ZG", "Ziguinchor"), ("LG", "Louga"), ("FK", "Fatick")
    ];

    private static readonly string[] Colours =
    [
        "Blanc", "Gris", "Noir", "Argent", "Bleu", "Beige", "Rouge", "Vert"
    ];

    /// <summary>
    /// Contraseña de las cuentas de demostración.
    /// </summary>
    /// <remarks>
    /// Aleatoria y desechada: estas cuentas existen para figurar como vendedoras de los
    /// anuncios de prueba, nadie necesita entrar con ellas. ❌ Una contraseña fija aquí
    /// sería una puerta abierta —este repositorio es público— y quien la leyera podría
    /// publicar y negociar en producción haciéndose pasar por ellas.
    /// </remarks>
    private static string UnusablePassword() =>
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

    private sealed record DemoSeller(
        string Phone, string Name, AccountType Type, string Region, string City);

    private static readonly DemoSeller[] DemoSellers =
    [
        new("+221771000001", "Auto Dakar Services", AccountType.Professionnel, "DK", "Dakar"),
        new("+221771000002", "Sahel Motors",        AccountType.Professionnel, "TH", "Thiès"),
        new("+221771000003", "Mamadou Diop",        AccountType.Particulier,   "DK", "Rufisque"),
        new("+221771000004", "Fatou Ndiaye",        AccountType.Particulier,   "SL", "Saint-Louis"),
        new("+221771000005", "Ousmane Sarr",        AccountType.Particulier,   "KL", "Kaolack"),
        new("+221771000006", "Teranga Auto",        AccountType.Professionnel, "DK", "Pikine")
    ];

    /// <summary>
    /// Frontera entre las cuentas de demostración del producto anterior y las de
    /// Yoon u Auto.
    /// </summary>
    /// <remarks>
    /// La adaptación empezó el 15 de agosto de 2026: todo lo anterior es del seed viejo.
    /// Se usa la fecha y no la región porque la región es opcional al registrarse, y una
    /// cuenta real sin región no puede confundirse con una de demostración.
    /// </remarks>
    private static readonly DateTimeOffset MigrationStart =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    public async Task ReseedAsync(CancellationToken ct = default)
    {
        await RetireLegacyCatalogueAsync(ct);
        await RehomeListingsAsync(ct);
        await EnsureAdminAsync(ct);
    }

    /// <summary>
    /// Garantiza que exista un administrador con el que se pueda entrar de verdad.
    /// </summary>
    /// <remarks>
    /// El seed del producto anterior creó una cuenta de administración con un hash
    /// inventado, así que nadie podía iniciar sesión con ella y el backoffice quedaba
    /// inaccesible.
    ///
    /// ❌ Aquí no se escribe ninguna contraseña: este repositorio es público y cualquiera
    /// podría leerla y entrar al backoffice de producción. Lo que se hace es
    /// <b>promover una cuenta que ya existe</b>, creada por su dueño desde el formulario
    /// de registro, cuya contraseña solo conoce quien la eligió.
    ///
    /// El teléfono se lee de la configuración (<c>Seed:AdminPhone</c>); sin él, el
    /// método se limita a avisar de que no hay administrador utilizable.
    /// </remarks>
    private async Task EnsureAdminAsync(CancellationToken ct)
    {
        var phone = adminPhone;

        // Se comprueba antes de consultar: con el teléfono vacío, la consulta buscaría
        // una cuenta sin teléfono en lugar de no buscar nada.
        var candidate = string.IsNullOrWhiteSpace(phone)
            ? null
            : await db.UserProfiles.FirstOrDefaultAsync(u => u.Phone == phone, ct);

        if (candidate is null)
        {
            var hasAdmin = await db.UserProfiles.AnyAsync(u => u.Role == UserRole.Admin, ct);

            if (!hasAdmin)
            {
                logger.LogWarning(
                    "⚠ No hay ningún administrador. Registre una cuenta y ponga su " +
                    "teléfono en Seed:AdminPhone para promoverla.");
            }
            return;
        }

        if (candidate.Role == UserRole.Admin) return;

        candidate.Role = UserRole.Admin;
        candidate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "✓ Cuenta {Phone} promovida a administrador", phone);
    }

    private async Task RetireLegacyCatalogueAsync(CancellationToken ct)
    {
        var legacy = await db.Vehicles
            .Where(v => v.CountryOrigin != "SN")
            .ToListAsync(ct);

        if (legacy.Count == 0)
        {
            logger.LogInformation("· Catálogo ya senegalés: no hay nada que sustituir");
            return;
        }

        logger.LogInformation(
            "🇸🇳 Sustituyendo el catálogo heredado: {Count} anuncios de fuera de Senegal",
            legacy.Count);

        var now = DateTimeOffset.UtcNow;

        foreach (var vehicle in legacy)
        {
            vehicle.DeletedAt = now;
            vehicle.UpdatedAt = now;
        }

        var sellers = await ResolveSellersAsync(now, ct);
        var (makes, models) = await EnsureCatalogueAsync(now, ct);

        var created = await CreateListingsAsync(makes, models, sellers, now, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "✓ Catálogo senegalés: {Retired} anuncios retirados, {Created} publicados",
            legacy.Count, created);
    }

    /// <summary>
    /// Pone los anuncios senegaleses en manos de vendedores senegaleses.
    /// </summary>
    /// <remarks>
    /// La primera pasada reutilizó las cuentas que ya había, y las que había eran las de
    /// demostración del producto anterior: un pick-up en Mbour aparecía vendido por
    /// «Marie Dubois, Lyon». Aquí se corrige y se retiran esas cuentas, que además
    /// falseaban el reparto por región de las estadísticas.
    /// </remarks>
    private async Task RehomeListingsAsync(CancellationToken ct)
    {
        var legacyUserIds = await db.UserProfiles
            .Where(u => u.Role != UserRole.Admin && u.CreatedAt < MigrationStart)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (legacyUserIds.Count == 0) return;

        var misplaced = await db.Vehicles
            .Where(v => v.CountryOrigin == "SN" && legacyUserIds.Contains(v.SellerId))
            .ToListAsync(ct);

        if (misplaced.Count == 0) return;

        logger.LogInformation(
            "🇸🇳 Reasignando {Count} anuncios que colgaban de cuentas heredadas",
            misplaced.Count);

        var now = DateTimeOffset.UtcNow;
        var sellers = await EnsureSenegaleseSellersAsync(now, ct);
        var rng = new Random(2026);

        foreach (var vehicle in misplaced)
        {
            var seller = sellers[rng.Next(sellers.Count)];

            vehicle.SellerId = seller.Id;
            // El anuncio se queda donde está el coche; lo que se corrige es quién lo
            // vende, no dónde se ve.
            vehicle.UpdatedAt = now;
        }

        // Las cuentas heredadas se retiran: sin ellas, el reparto por región y el
        // recuento de particuliers y professionnels vuelven a ser ciertos.
        var legacyUsers = await db.UserProfiles
            .Where(u => legacyUserIds.Contains(u.Id))
            .ToListAsync(ct);

        foreach (var user in legacyUsers)
        {
            user.DeletedAt = now;
            user.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "✓ {Count} anuncios reasignados · {Users} cuentas heredadas retiradas",
            misplaced.Count, legacyUsers.Count);
    }

    /// <summary>Los vendedores de demostración, creados solo si faltan.</summary>
    private async Task<List<UserProfile>> EnsureSenegaleseSellersAsync(
        DateTimeOffset now, CancellationToken ct)
    {
        var phones = DemoSellers.Select(d => d.Phone).ToList();

        var existing = await db.UserProfiles
            .Where(u => phones.Contains(u.Phone))
            .ToListAsync(ct);

        var missing = DemoSellers
            .Where(d => !existing.Any(u => u.Phone == d.Phone))
            .ToList();

        if (missing.Count == 0) return existing;


        var created = missing.Select(d => new UserProfile
        {
            Phone = d.Phone,
            DisplayName = d.Name,
            PasswordHash = UnusablePassword(),
            AccountType = d.Type,
            Region = d.Region,
            City = d.City,
            PhoneVerified = true,
            CreatedAt = now.AddDays(-60),
            UpdatedAt = now,
            LastLoginAt = now.AddDays(-2)
        }).ToList();

        db.UserProfiles.AddRange(created);
        await db.SaveChangesAsync(ct);

        return [.. existing, .. created];
    }

    /// <summary>
    /// Quién publica los anuncios.
    /// </summary>
    /// <remarks>
    /// Siempre cuentas senegalesas: reutilizar las que hubiera dejaba pick-ups de Mbour
    /// vendidos desde Lyon.
    /// </remarks>
    private Task<List<UserProfile>> ResolveSellersAsync(
        DateTimeOffset now, CancellationToken ct) => EnsureSenegaleseSellersAsync(now, ct);

    /// <summary>Marcas y modelos del catálogo, creando solo lo que falte.</summary>
    private async Task<(List<VehicleMake>, List<VehicleModel>)> EnsureCatalogueAsync(
        DateTimeOffset now, CancellationToken ct)
    {
        var makes = await db.VehicleMakes.ToListAsync(ct);
        var models = await db.VehicleModels.ToListAsync(ct);

        foreach (var spec in Catalogue)
        {
            var make = makes.FirstOrDefault(
                m => string.Equals(m.Name, spec.Make, StringComparison.OrdinalIgnoreCase));

            if (make is null)
            {
                make = new VehicleMake
                {
                    Name = spec.Make, IsPopular = true,
                    CreatedAt = now, UpdatedAt = now
                };
                db.VehicleMakes.Add(make);
                makes.Add(make);
            }

            var model = models.FirstOrDefault(
                m => m.MakeId == make.Id
                  && string.Equals(m.Name, spec.Model, StringComparison.OrdinalIgnoreCase));

            if (model is null)
            {
                model = new VehicleModel
                {
                    MakeId = make.Id, Name = spec.Model,
                    CreatedAt = now, UpdatedAt = now
                };
                db.VehicleModels.Add(model);
                models.Add(model);
            }
        }

        await db.SaveChangesAsync(ct);
        return (makes, models);
    }

    private async Task<int> CreateListingsAsync(
        List<VehicleMake> makes,
        List<VehicleModel> models,
        List<UserProfile> sellers,
        DateTimeOffset now,
        CancellationToken ct)
    {
        // Semilla fija: dos ejecuciones producen el mismo catálogo, lo que hace que un
        // fallo se pueda reproducir.
        var rng = new Random(2026);

        var lastReference = await db.Vehicles
            .Where(v => v.PublicReference.StartsWith("YU"))
            .Select(v => v.PublicReference)
            .ToListAsync(ct);

        var nextRef = lastReference
            .Select(r => int.TryParse(r[2..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var vehicles = new List<Vehicle>();
        var images = new List<VehicleImage>();
        var slugs = new HashSet<string>(
            await db.Vehicles.IgnoreQueryFilters().Select(v => v.Slug).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        foreach (var spec in Catalogue)
        {
            var make = makes.First(
                m => string.Equals(m.Name, spec.Make, StringComparison.OrdinalIgnoreCase));
            var model = models.First(
                m => m.MakeId == make.Id
                  && string.Equals(m.Name, spec.Model, StringComparison.OrdinalIgnoreCase));

            // Seis ejemplares por modelo: uno por encima del mínimo de comparables, para
            // que el indicador de precio tenga con qué trabajar aunque uno se archive.
            for (var i = 0; i < 6; i++)
            {
                var year = spec.BaseYear + rng.Next(-2, 3);
                var mileage = spec.BaseMileage + rng.Next(-35_000, 40_000);

                // El precio se mueve alrededor de la referencia según año y kilometraje,
                // más un margen del vendedor: así la mediana es creíble y hay ejemplares
                // claramente baratos y claramente caros.
                var yearEffect = (year - spec.BaseYear) * 0.06m;
                var kmEffect = (spec.BaseMileage - mileage) / 1_000_000m;
                var spread = (decimal)(rng.NextDouble() * 0.24 - 0.12);

                var price = Math.Round(
                    spec.BasePrice * (1 + yearEffect + kmEffect + spread) / 50_000m, 0) * 50_000m;

                var (region, city) = Places[rng.Next(Places.Length)];
                var seller = sellers[rng.Next(sellers.Count)];
                var colour = Colours[rng.Next(Colours.Length)];

                var slug = UniqueSlug(slugs, spec.Make, spec.Model, year);
                var publishedAt = now.AddDays(-rng.Next(1, 75));

                var vehicle = new Vehicle
                {
                    PublicReference = $"YU{nextRef++:D5}",
                    Slug = slug,
                    Title = $"{spec.Make} {spec.Model} {year}",
                    Description =
                        $"{spec.Make} {spec.Model} de {year}, {mileage:N0} km. " +
                        $"Entretien suivi, pneus en bon état, climatisation fonctionnelle. " +
                        $"Visible à {city}. Papiers en règle.",
                    MakeId = make.Id,
                    ModelId = model.Id,
                    Year = year,
                    Mileage = Math.Max(5_000, mileage),
                    Condition = VehicleCondition.Used,
                    BodyType = spec.Body,
                    FuelType = spec.Fuel,
                    Transmission = spec.Transmission,
                    Color = colour,
                    Doors = spec.Body == BodyType.Citadine ? 3 : 5,
                    Seats = spec.Body == BodyType.PickUp ? 5 : 5,
                    PowerCv = rng.Next(90, 190),
                    EngineDisplacementCc = rng.Next(1400, 2800),
                    Drivetrain = spec.Body is BodyType.Suv or BodyType.PickUp
                        ? Drivetrain.Integrale
                        : Drivetrain.Avant,
                    // El estado aduanero es el filtro que más importa en Senegal: se
                    // reparte para que los tres valores tengan anuncios.
                    CustomsStatus = (CustomsStatus)(1 + (i % 3)),
                    Price = price,
                    Currency = "XOF",
                    PriceNegotiable = rng.NextDouble() > 0.5,
                    CountryOrigin = "SN",
                    Region = region,
                    City = city,
                    Status = VehicleStatus.Actif,
                    IsFeatured = i == 0,
                    ViewsCount = rng.Next(20, 900),
                    FavoritesCount = rng.Next(0, 25),
                    ContactsCount = rng.Next(0, 12),
                    SellerId = seller.Id,
                    PublishedAt = publishedAt,
                    CreatedAt = publishedAt,
                    UpdatedAt = now
                };

                // Punto inicial del histórico de precios: sin él no hay «évolution».
                vehicle.PriceHistory.Add(new VehiclePriceHistory
                {
                    VehicleId = vehicle.Id,
                    Price = vehicle.Price,
                    ChangedAt = vehicle.CreatedAt
                });

                vehicles.Add(vehicle);

                images.Add(new VehicleImage
                {
                    VehicleId = vehicle.Id,
                    // Imagen remota: el disco de Render es efímero y cualquier archivo
                    // subido desaparece al reiniciar.
                    Url = $"https://picsum.photos/seed/{vehicle.Id:N}/1200/800",
                    ThumbnailUrl = $"https://picsum.photos/seed/{vehicle.Id:N}/400/300",
                    SortOrder = 0,
                    IsPrimary = true,
                    AltText = $"{spec.Make} {spec.Model} {year}",
                    Width = 1200,
                    Height = 800,
                    Format = "jpeg",
                    CreatedAt = vehicle.CreatedAt,
                    UpdatedAt = vehicle.CreatedAt
                });
            }
        }

        db.Vehicles.AddRange(vehicles);
        db.VehicleImages.AddRange(images);

        return vehicles.Count;
    }

    private static string UniqueSlug(HashSet<string> taken, string make, string model, int year)
    {
        var basis = $"{make}-{model}-{year}"
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "");

        var slug = basis;
        var suffix = 2;

        while (!taken.Add(slug))
        {
            slug = $"{basis}-{suffix++}";
        }

        return slug;
    }
}
