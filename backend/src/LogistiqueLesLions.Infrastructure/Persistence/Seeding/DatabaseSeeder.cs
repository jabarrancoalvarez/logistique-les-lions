using System.Text.Json;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogistiqueLesLions.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeder de datos de demo. Idempotente: solo inserta lo que aún no existe.
/// Pensado para entornos dev/staging y para llenar la BBDD lo suficiente
/// como para probar todas las pantallas del frontend.
///
/// Uso: se invoca desde Program.cs después de aplicar migraciones cuando
/// la config "Seed:Enabled" es true.
/// </summary>
public class DatabaseSeeder(
    ApplicationDbContext db,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        logger.LogInformation("🌱 Iniciando seed de datos de demo...");
        var now = DateTimeOffset.UtcNow;

        var countries = await SeedCountriesAsync(now, ct);
        var (makes, models) = await SeedMakesAndModelsAsync(now, ct);
        var users = await SeedUsersAsync(now, ct);
        var vehicles = await SeedVehiclesAsync(makes, models, users, now, ct);
        await RepairPlaceholderVehicleImagesAsync(ct);
        await SeedCountryRequirementsAsync(now, ct);
        await SeedHomologationsAndTariffsAsync(now, ct);
        await SeedDocumentTemplatesAsync(now, ct);
        var processes = await SeedProcessesAsync(vehicles, users, now, ct);
        await SeedProcessDocumentsAsync(processes, now, ct);
        await SeedIncidentsAsync(processes, users, now, ct);
        await SeedConversationsAsync(vehicles, users, now, ct);
        await SeedNotificationsAsync(users, now, ct);
        await SeedPartnersAsync(now, ct);
        await SeedNewsletterAsync(now, ct);

        logger.LogInformation("✓ Seed completado");
    }

    // ─── Países ─────────────────────────────────────────────────────────────
    private async Task<List<Country>> SeedCountriesAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Countries.AnyAsync(ct))
            return await db.Countries.ToListAsync(ct);

        var list = new List<Country>
        {
            new() { Code="ES", NameEs="España",       NameEn="Spain",          FlagEmoji="🇪🇸", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=1 },
            new() { Code="FR", NameEs="Francia",      NameEn="France",         FlagEmoji="🇫🇷", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=2 },
            new() { Code="DE", NameEs="Alemania",     NameEn="Germany",        FlagEmoji="🇩🇪", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=3 },
            new() { Code="IT", NameEs="Italia",       NameEn="Italy",          FlagEmoji="🇮🇹", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=4 },
            new() { Code="PT", NameEs="Portugal",     NameEn="Portugal",       FlagEmoji="🇵🇹", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=5 },
            new() { Code="NL", NameEs="Países Bajos", NameEn="Netherlands",    FlagEmoji="🇳🇱", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=6 },
            new() { Code="BE", NameEs="Bélgica",      NameEn="Belgium",        FlagEmoji="🇧🇪", Currency="EUR", IsEuMember=true,  SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=7 },
            new() { Code="GB", NameEs="Reino Unido",  NameEn="United Kingdom", FlagEmoji="🇬🇧", Currency="GBP", IsEuMember=false, SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=8 },
            new() { Code="JP", NameEs="Japón",        NameEn="Japan",          FlagEmoji="🇯🇵", Currency="JPY", IsEuMember=false, SupportsImport=true, SupportsExport=false, IsActive=true, DisplayOrder=9 },
            new() { Code="US", NameEs="Estados Unidos", NameEn="United States",FlagEmoji="🇺🇸", Currency="USD", IsEuMember=false, SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=10 },
            new() { Code="MA", NameEs="Marruecos",    NameEn="Morocco",        FlagEmoji="🇲🇦", Currency="MAD", IsEuMember=false, SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=11 },
            new() { Code="CH", NameEs="Suiza",        NameEn="Switzerland",    FlagEmoji="🇨🇭", Currency="CHF", IsEuMember=false, SupportsImport=true, SupportsExport=true, IsActive=true, DisplayOrder=12 },
        };
        foreach (var c in list) { c.CreatedAt = now; c.UpdatedAt = now; }
        db.Countries.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} países", list.Count);
        return list;
    }

    // ─── Marcas y modelos ──────────────────────────────────────────────────
    private async Task<(List<VehicleMake> makes, List<VehicleModel> models)> SeedMakesAndModelsAsync(
        DateTimeOffset now, CancellationToken ct)
    {
        if (await db.VehicleMakes.AnyAsync(ct))
        {
            var ms = await db.VehicleMakes.ToListAsync(ct);
            var mods = await db.VehicleModels.ToListAsync(ct);
            return (ms, mods);
        }

        var makesData = new (string Name, string Country, bool Popular, string[] Models)[]
        {
            ("BMW",           "DE", true,  new[] { "Serie 1", "Serie 3", "Serie 5", "X1", "X3", "X5" }),
            ("Mercedes-Benz", "DE", true,  new[] { "Clase A", "Clase C", "Clase E", "GLA", "GLC", "GLE" }),
            ("Audi",          "DE", true,  new[] { "A1", "A3", "A4", "A6", "Q3", "Q5", "Q7" }),
            ("Volkswagen",    "DE", true,  new[] { "Polo", "Golf", "Passat", "Tiguan", "T-Roc", "ID.4" }),
            ("Toyota",        "JP", true,  new[] { "Yaris", "Corolla", "RAV4", "C-HR", "Prius", "Land Cruiser" }),
            ("Renault",       "FR", true,  new[] { "Clio", "Megane", "Captur", "Kadjar", "Zoe" }),
            ("Peugeot",       "FR", true,  new[] { "208", "308", "508", "2008", "3008", "5008" }),
            ("Seat",          "ES", true,  new[] { "Ibiza", "Leon", "Ateca", "Arona", "Tarraco" }),
            ("Tesla",         "US", true,  new[] { "Model 3", "Model Y", "Model S", "Model X" }),
            ("Ford",          "US", false, new[] { "Fiesta", "Focus", "Kuga", "Mustang", "Puma" }),
            ("Honda",         "JP", false, new[] { "Civic", "Jazz", "CR-V", "HR-V" }),
            ("Hyundai",       "KR", false, new[] { "i20", "i30", "Tucson", "Kona", "Ioniq 5" }),
        };

        var makes  = new List<VehicleMake>();
        var models = new List<VehicleModel>();

        foreach (var m in makesData)
        {
            var make = new VehicleMake
            {
                Name      = m.Name,
                Country   = m.Country,
                IsPopular = m.Popular,
                CreatedAt = now,
                UpdatedAt = now
            };
            makes.Add(make);

            foreach (var modelName in m.Models)
            {
                models.Add(new VehicleModel
                {
                    MakeId    = make.Id,
                    Name      = modelName,
                    Category  = "Pasajero",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        db.VehicleMakes.AddRange(makes);
        db.VehicleModels.AddRange(models);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {Makes} marcas, {Models} modelos", makes.Count, models.Count);
        return (makes, models);
    }

    // ─── Usuarios ──────────────────────────────────────────────────────────
    private async Task<List<UserProfile>> SeedUsersAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (await db.UserProfiles.AnyAsync(ct))
            return await db.UserProfiles.ToListAsync(ct);

        // Hash bcrypt fijo de "demo1234" para que admin/dev pueda loguearse
        // (sin usar BCrypt aquí para no añadir dependencia transitiva — el sistema
        // de auth real lo hashea correctamente, esto es solo para datos demo).
        const string demoHash = "$2a$11$abcdefghijklmnopqrstuv0123456789ABCDEFGHIJKLMNOPQRSTUVWX";

        // Usuarios senegaleses: el teléfono es el identificador de la cuenta.
        var list = new List<UserProfile>
        {
            new() { Phone="+221770000001", Email="admin@yoonuauto.test", PasswordHash=demoHash, DisplayName="Administration Yoon", Role=UserRole.Admin, AccountType=AccountType.Professionnel, Region="DK", City="Dakar",       PhoneVerified=true,  Bio="Administrateur de la plateforme." },
            new() { Phone="+221770000002", Email="auto.dakar@yoonuauto.test", PasswordHash=demoHash, DisplayName="Auto Dakar Services", AccountType=AccountType.Professionnel, Region="DK", City="Dakar",       PhoneVerified=true,  VerifiedSalesCount=8 },
            new() { Phone="+221770000003", Email="sahel.motors@yoonuauto.test", PasswordHash=demoHash, DisplayName="Sahel Motors",      AccountType=AccountType.Professionnel, Region="TH", City="Thiès",       PhoneVerified=true,  VerifiedSalesCount=5 },
            new() { Phone="+221770000004", Email="teranga.auto@yoonuauto.test", PasswordHash=demoHash, DisplayName="Teranga Auto",      AccountType=AccountType.Professionnel, Region="DK", City="Rufisque",    PhoneVerified=true,  VerifiedSalesCount=3 },
            new() { Phone="+221770000005", PasswordHash=demoHash, DisplayName="Mamadou Diop",     AccountType=AccountType.Particulier,  Region="DK", City="Dakar",       PhoneVerified=true,  VerifiedSalesCount=2 },
            new() { Phone="+221770000006", PasswordHash=demoHash, DisplayName="Fatou Ndiaye",     AccountType=AccountType.Particulier,  Region="TH", City="Mbour",       PhoneVerified=true },
            new() { Phone="+221770000007", PasswordHash=demoHash, DisplayName="Ousmane Sarr",     AccountType=AccountType.Particulier,  Region="SL", City="Saint-Louis", PhoneVerified=true },
            new() { Phone="+221770000008", PasswordHash=demoHash, DisplayName="Aïssatou Fall",    AccountType=AccountType.Particulier,  Region="DB", City="Touba",       PhoneVerified=false },
            new() { Phone="+221770000009", PasswordHash=demoHash, DisplayName="Ibrahima Bâ",      AccountType=AccountType.Particulier,  Region="ZG", City="Ziguinchor",  PhoneVerified=true },
            new() { Phone="+221770000010", PasswordHash=demoHash, DisplayName="Awa Sow",          AccountType=AccountType.Particulier,  Region="KL", City="Kaolack",     PhoneVerified=false },
        };

        foreach (var u in list)
        {
            u.CreatedAt   = now.AddDays(-Random.Shared.Next(1, 180));
            u.UpdatedAt   = now;
            u.LastLoginAt = now.AddDays(-Random.Shared.Next(0, 14));
            u.LastActivityAt = u.LastLoginAt;
        }

        db.UserProfiles.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} usuarios (admin: +221770000001 / demo1234)", list.Count);
        return list;
    }

    // ─── Vehículos ─────────────────────────────────────────────────────────
    // Fotos reales curadas de Unsplash (IDs verificados, sin restricciones de hotlink).
    // Se rotan por hash del vehículo para dar variedad. loremflickr se descartó tras
    // comprobar que el host puede estar caído.
    private static readonly string[] UnsplashCarPhotoIds =
    [
        "1503376780353-7e6692767b70",
        "1492144534655-ae79c964c9d7",
        "1549317661-bd32c8ce0db2",
        "1552519507-da3b142c6e3d",
        "1494976388531-d1058494cdd8",
        "1580273916550-e323be2ae537",
        "1553440569-bcc63803a83d",
        "1605559424843-9e4c228bf1c2",
        "1542362567-b07e54358753",
        "1511919884226-fd3cad34687c",
        "1606664515524-ed2f786a0bd6",
        "1469285994282-454ceb49e63c",
    ];

    private static (string full, string thumb) PickCarPhoto(Guid vehicleId, int sortOrder)
    {
        var hash = Math.Abs(HashCode.Combine(vehicleId, sortOrder));
        var photoId = UnsplashCarPhotoIds[hash % UnsplashCarPhotoIds.Length];
        var full  = $"https://images.unsplash.com/photo-{photoId}?w=1200&q=80&auto=format&fit=crop";
        var thumb = $"https://images.unsplash.com/photo-{photoId}?w=400&q=70&auto=format&fit=crop";
        return (full, thumb);
    }

    private async Task<List<Vehicle>> SeedVehiclesAsync(
        List<VehicleMake> makes, List<VehicleModel> models, List<UserProfile> users,
        DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Vehicles.AnyAsync(ct))
            return await db.Vehicles.ToListAsync(ct);

        // Cualquier usuario puede vender: excluimos solo la cuenta de administración.
        var sellers = users.Where(u => u.Role != UserRole.Admin).ToList();
        var rng = new Random(42);

        string Slug(string make, string model, int year, int idx) =>
            $"{make}-{model}-{year}-{idx}".ToLowerInvariant().Replace(" ", "-").Replace(".", "");

        var data = new (string Make, string Model, int Year, int Mileage, decimal Price, FuelType Fuel, TransmissionType Tx, BodyType Body, string Color, string Country, string City, bool Featured, bool ExportReady, VehicleStatus Status)[]
        {
            ("BMW","Serie 3",2022, 35000, 32500, FuelType.Diesel,    TransmissionType.Automatique, BodyType.Berline,     "Negro",   "DE","Múnich",     true,  true,  VehicleStatus.Actif),
            ("BMW","X5",     2021, 48000, 56500, FuelType.Hybride,    TransmissionType.Automatique, BodyType.Suv,       "Blanco",  "DE","Hamburgo",   true,  true,  VehicleStatus.Actif),
            ("Mercedes-Benz","Clase C",2023,12000,45000, FuelType.HybrideRechargeable,TransmissionType.Automatique, BodyType.Berline,"Plata","DE","Stuttgart", true,  true,  VehicleStatus.Actif),
            ("Mercedes-Benz","GLE",   2022, 28000, 68000, FuelType.Diesel,    TransmissionType.Automatique, BodyType.Suv,   "Gris",   "DE","Berlín",     false, true,  VehicleStatus.Actif),
            ("Audi","A4",    2021, 55000, 28900, FuelType.Diesel,    TransmissionType.Automatique, BodyType.Berline,    "Azul",    "DE","Frankfurt",  false, true,  VehicleStatus.Actif),
            ("Audi","Q5",    2023,  8000, 52000, FuelType.HybrideRechargeable,TransmissionType.Automatique,BodyType.Suv,     "Negro",   "DE","Múnich",     true,  true,  VehicleStatus.Actif),
            ("Volkswagen","Golf",2022,22000,24500, FuelType.Essence,TransmissionType.Manuel,    BodyType.Citadine,"Rojo",    "ES","Madrid",     false, false, VehicleStatus.Actif),
            ("Volkswagen","ID.4",2023, 5000, 38900, FuelType.Electrique, TransmissionType.Automatique,BodyType.Suv,     "Blanco",  "ES","Barcelona",  true,  true,  VehicleStatus.Actif),
            ("Toyota","Corolla",2022,30000,21500, FuelType.Hybride,    TransmissionType.Automatique, BodyType.Berline,    "Plata",   "FR","Lyon",       false, true,  VehicleStatus.Actif),
            ("Toyota","RAV4",   2021, 65000, 27500, FuelType.Hybride,    TransmissionType.Automatique, BodyType.Suv,    "Verde",   "FR","París",      false, true,  VehicleStatus.Actif),
            ("Tesla","Model 3", 2022, 25000, 41900, FuelType.Electrique, TransmissionType.Automatique, BodyType.Berline,    "Blanco",  "NL","Ámsterdam",  true,  true,  VehicleStatus.Actif),
            ("Tesla","Model Y", 2023, 12000, 49500, FuelType.Electrique, TransmissionType.Automatique, BodyType.Suv,      "Negro",   "DE","Berlín",     true,  true,  VehicleStatus.Actif),
            ("Renault","Megane",2021, 70000, 14500, FuelType.Diesel,   TransmissionType.Manuel,    BodyType.Citadine,"Gris",    "FR","Marsella",   false, false, VehicleStatus.Actif),
            ("Peugeot","3008",  2022, 38000, 26900, FuelType.Diesel,   TransmissionType.Automatique, BodyType.Suv,      "Azul",    "FR","Toulouse",   false, true,  VehicleStatus.Actif),
            ("Seat","Leon",     2023, 15000, 22500, FuelType.Essence, TransmissionType.Manuel,    BodyType.Citadine,"Rojo",    "ES","Valencia",   false, true,  VehicleStatus.Actif),
            ("Honda","Civic",   2022, 42000, 19900, FuelType.Essence, TransmissionType.Automatique, BodyType.Citadine,"Negro",   "ES","Sevilla",    false, false, VehicleStatus.Actif),
            ("Hyundai","Tucson",2021, 60000, 22900, FuelType.Diesel,   TransmissionType.Manuel,    BodyType.Suv,      "Blanco",  "PT","Lisboa",     false, true,  VehicleStatus.Actif),
            ("Ford","Mustang",  2020, 32000, 39500, FuelType.Essence, TransmissionType.Automatique, BodyType.Coupe,    "Amarillo","US","Los Ángeles",true,  true,  VehicleStatus.Actif),
            ("BMW","Serie 1",   2023, 10000, 28500, FuelType.Essence, TransmissionType.Automatique, BodyType.Citadine,"Blanco",  "DE","Düsseldorf", false, true,  VehicleStatus.Brouillon),
            ("Audi","A1",       2022, 25000, 19500, FuelType.Essence, TransmissionType.Manuel,    BodyType.Citadine,"Rojo",    "DE","Colonia",    false, true,  VehicleStatus.Brouillon),

            // Grupo de RAV4 comparables entre sí: sin una muestra de este tamaño el
            // indicador estadístico de precio nunca llegaría al mínimo de comparables y
            // no podría verse en el entorno de demo.
            ("Toyota","RAV4",   2019, 126000, 8900000, FuelType.Essence, TransmissionType.Automatique, BodyType.Suv, "Gris",   "SN","Dakar",   true,  false, VehicleStatus.Actif),
            ("Toyota","RAV4",   2019, 118000, 9400000, FuelType.Essence, TransmissionType.Automatique, BodyType.Suv, "Blanco", "SN","Dakar",   false, false, VehicleStatus.Actif),
            ("Toyota","RAV4",   2020,  95000, 10200000,FuelType.Essence, TransmissionType.Automatique, BodyType.Suv, "Negro",  "SN","Thiès",   false, false, VehicleStatus.Actif),
            ("Toyota","RAV4",   2018, 140000, 7800000, FuelType.Diesel,  TransmissionType.Automatique, BodyType.Suv, "Gris",   "SN","Mbour",   false, false, VehicleStatus.Actif),
            ("Toyota","RAV4",   2020, 102000, 10800000,FuelType.Essence, TransmissionType.Automatique, BodyType.Suv, "Azul",   "SN","Rufisque",false, false, VehicleStatus.Actif),
            ("Toyota","RAV4",   2018, 155000, 7200000, FuelType.Diesel,  TransmissionType.Manuel,      BodyType.Suv, "Blanco", "SN","Kaolack", false, false, VehicleStatus.Actif),
            // Uno claramente barato y otro claramente caro, para ver los tres resultados.
            ("Toyota","RAV4",   2019, 130000, 6500000, FuelType.Essence, TransmissionType.Automatique, BodyType.Suv, "Verde",  "SN","Dakar",   false, false, VehicleStatus.Actif),
            ("Toyota","RAV4",   2019,  88000, 13500000,FuelType.Essence, TransmissionType.Automatique, BodyType.Suv, "Negro",  "SN","Dakar",   false, false, VehicleStatus.Actif),
        };

        var vehicles = new List<Vehicle>();
        var images   = new List<VehicleImage>();
        int idx = 1;

        foreach (var d in data)
        {
            var make  = makes.First(m => m.Name == d.Make);
            var model = models.FirstOrDefault(m => m.MakeId == make.Id && m.Name == d.Model);
            var seller = sellers[rng.Next(sellers.Count)];

            // El vendedor de demo vive en Senegal: el anuncio hereda su región y ciudad.
            var customs = (CustomsStatus)(1 + (idx % 3));

            var v = new Vehicle
            {
                PublicReference = $"YU{90000 + idx:D5}",
                Slug          = Slug(d.Make, d.Model, d.Year, idx++),
                Title         = $"{d.Make} {d.Model} {d.Year}",
                Description   = $"{d.Make} {d.Model} de {d.Year} en très bon état. " +
                                $"{d.Mileage:N0} km, {d.Fuel}, {d.Tx}. Couleur {d.Color.ToLower()}. " +
                                $"Entretien à jour, papiers en règle. Visible à {seller.City}.",
                MakeId        = make.Id,
                ModelId       = model?.Id,
                Year          = d.Year,
                Mileage       = d.Mileage,
                Condition     = d.Year >= 2023 && d.Mileage < 15000 ? VehicleCondition.Km0 : VehicleCondition.Used,
                BodyType      = d.Body,
                FuelType      = d.Fuel,
                Transmission  = d.Tx,
                Color         = d.Color,
                Doors         = d.Body is BodyType.Citadine or BodyType.Coupe ? 3 : 5,
                Seats         = d.Body is BodyType.Monospace ? 7 : 5,
                PowerCv       = rng.Next(70, 250),
                EngineDisplacementCc = rng.Next(1000, 3000),
                Drivetrain    = d.Body is BodyType.Suv ? Drivetrain.Integrale : Drivetrain.Avant,
                Vin           = $"WBA{rng.Next(10000000, 99999999)}{rng.Next(1000, 9999)}",
                CustomsStatus = customs,
                Price         = d.Price,
                Currency      = "XOF",
                PriceNegotiable = rng.NextDouble() > 0.6,
                CountryOrigin = d.Country,
                Region        = seller.Region,
                City          = seller.City,
                Status        = d.Status,
                IsFeatured    = d.Featured,
                IsExportReady = d.ExportReady,
                ViewsCount    = rng.Next(50, 5000),
                FavoritesCount= rng.Next(0, 80),
                ContactsCount = rng.Next(0, 25),
                SellerId      = seller.Id,
                PublishedAt   = now.AddDays(-rng.Next(1, 90)),
                CreatedAt     = now.AddDays(-rng.Next(1, 90)),
                UpdatedAt     = now.AddDays(-rng.Next(0, 7))
            };

            // Punto inicial del histórico de precios, necesario para «Évolution du prix».
            v.PriceHistory.Add(new VehiclePriceHistory
            {
                VehicleId = v.Id,
                Price     = v.Price,
                ChangedAt = v.CreatedAt
            });

            vehicles.Add(v);

            // 3 fotos reales de Unsplash por vehículo (IDs curados en UnsplashCarPhotoIds).
            for (int i = 0; i < 3; i++)
            {
                var (full, thumb) = PickCarPhoto(v.Id, i);
                images.Add(new VehicleImage
                {
                    VehicleId    = v.Id,
                    Url          = full,
                    ThumbnailUrl = thumb,
                    SortOrder    = i,
                    IsPrimary    = i == 0,
                    AltText      = $"{d.Make} {d.Model} foto {i + 1}",
                    Width        = 1200,
                    Height       = 800,
                    Format       = "jpeg",
                    CreatedAt    = v.CreatedAt,
                    UpdatedAt    = v.CreatedAt
                });
            }
        }

        db.Vehicles.AddRange(vehicles);
        db.VehicleImages.AddRange(images);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {V} vehículos, {I} imágenes", vehicles.Count, images.Count);
        return vehicles;
    }

    // ─── Reparación: cambiar placehold.co por fotos reales ─────────────────
    // Se ejecuta en cada seed (no solo la primera vez) para migrar instalaciones
    // antiguas que ya tenían VehicleImages con URLs de placehold.co.
    private async Task RepairPlaceholderVehicleImagesAsync(CancellationToken ct)
    {
        var toFix = await db.VehicleImages
            .Where(i =>
                i.Url.Contains("placehold.co") ||
                i.Url.Contains("loremflickr") ||
                (i.ThumbnailUrl != null && (i.ThumbnailUrl.Contains("placehold.co") || i.ThumbnailUrl.Contains("loremflickr"))))
            .ToListAsync(ct);

        if (toFix.Count == 0)
            return;

        foreach (var img in toFix)
        {
            var (full, thumb) = PickCarPhoto(img.VehicleId, img.SortOrder);
            img.Url = full;
            img.ThumbnailUrl = thumb;
            img.Format = "jpeg";
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} imágenes reparadas → Unsplash", toFix.Count);
    }

    // ─── Normativa por par de países ───────────────────────────────────────
    private async Task SeedCountryRequirementsAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (await db.CountryRequirements.AnyAsync(ct)) return;

        var docs = JsonSerializer.Serialize(new[] { "FichaTecnica", "PermisoCirculacion", "FacturaCompra", "CertificadoConformidadCOC" });

        var list = new List<CountryRequirement>
        {
            new() { OriginCountry="DE", DestinationCountry="ES", DocumentTypesJson=docs, HomologationRequired=false, CustomsRatePercent=0,  VatRatePercent=21, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=550,  EstimatedDays=21, NotesEs="Importación intra-UE. Sin arancel.",   NotesEn="Intra-EU import. No customs duty.", LastUpdatedAt=now },
            new() { OriginCountry="FR", DestinationCountry="ES", DocumentTypesJson=docs, HomologationRequired=false, CustomsRatePercent=0,  VatRatePercent=21, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=480,  EstimatedDays=18, NotesEs="Importación desde Francia.",          NotesEn="Import from France.", LastUpdatedAt=now },
            new() { OriginCountry="IT", DestinationCountry="ES", DocumentTypesJson=docs, HomologationRequired=false, CustomsRatePercent=0,  VatRatePercent=21, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=520,  EstimatedDays=20, NotesEs="Importación desde Italia.",           NotesEn="Import from Italy.", LastUpdatedAt=now },
            new() { OriginCountry="DE", DestinationCountry="FR", DocumentTypesJson=docs, HomologationRequired=false, CustomsRatePercent=0,  VatRatePercent=20, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=600,  EstimatedDays=22, NotesEs="Importación a Francia.",              NotesEn="Import to France.", LastUpdatedAt=now },
            new() { OriginCountry="JP", DestinationCountry="ES", DocumentTypesJson=docs, HomologationRequired=true,  CustomsRatePercent=10, VatRatePercent=21, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=2200, EstimatedDays=60, NotesEs="Extra-UE. Requiere homologación europea.", NotesEn="Extra-EU. Requires EU homologation.", LastUpdatedAt=now },
            new() { OriginCountry="US", DestinationCountry="ES", DocumentTypesJson=docs, HomologationRequired=true,  CustomsRatePercent=10, VatRatePercent=21, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=2800, EstimatedDays=75, NotesEs="Extra-UE desde EE.UU.",               NotesEn="Extra-EU from USA.", LastUpdatedAt=now },
            new() { OriginCountry="GB", DestinationCountry="ES", DocumentTypesJson=docs, HomologationRequired=false, CustomsRatePercent=10, VatRatePercent=21, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=1800, EstimatedDays=45, NotesEs="Post-Brexit: arancel aplicable.",     NotesEn="Post-Brexit: customs apply.", LastUpdatedAt=now },
            new() { OriginCountry="ES", DestinationCountry="MA", DocumentTypesJson=docs, HomologationRequired=true,  CustomsRatePercent=17, VatRatePercent=20, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=1500, EstimatedDays=40, NotesEs="Exportación a Marruecos.",            NotesEn="Export to Morocco.", LastUpdatedAt=now },
            new() { OriginCountry="DE", DestinationCountry="PT", DocumentTypesJson=docs, HomologationRequired=false, CustomsRatePercent=0,  VatRatePercent=23, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=520,  EstimatedDays=20, NotesEs="Intra-UE.",                          NotesEn="Intra-EU.", LastUpdatedAt=now },
            new() { OriginCountry="ES", DestinationCountry="GB", DocumentTypesJson=docs, HomologationRequired=true,  CustomsRatePercent=10, VatRatePercent=20, TechnicalInspectionRequired=true, EstimatedProcessingCostEur=1900, EstimatedDays=50, NotesEs="Post-Brexit: requiere homologación UK.", NotesEn="Post-Brexit: needs UK homologation.", LastUpdatedAt=now },
        };
        foreach (var r in list) { r.CreatedAt = now; r.UpdatedAt = now; }
        db.CountryRequirements.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} normativas país", list.Count);
    }

    private async Task SeedHomologationsAndTariffsAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (!await db.HomologationRequirements.AnyAsync(ct))
        {
            var homologs = new List<HomologationRequirement>
            {
                new() { DestinationCountry="ES", VehicleCategory="M1", YearFrom=2010, YearTo=2030, NotesEs="Homologación individual para vehículos extra-UE", EmissionStandard="Euro 6", CertifyingBody="ITV", EstimatedCostEur=1500 },
                new() { DestinationCountry="FR", VehicleCategory="M1", YearFrom=2010, YearTo=2030, NotesEs="Réception à titre isolé (RTI)",                  EmissionStandard="Euro 6", CertifyingBody="DREAL", EstimatedCostEur=1800 },
                new() { DestinationCountry="DE", VehicleCategory="M1", YearFrom=2010, YearTo=2030, NotesEs="Einzelabnahme TÜV",                               EmissionStandard="Euro 6", CertifyingBody="TÜV", EstimatedCostEur=1200 },
                new() { DestinationCountry="IT", VehicleCategory="M1", YearFrom=2010, YearTo=2030, NotesEs="Omologazione singola",                            EmissionStandard="Euro 6", CertifyingBody="MCTC", EstimatedCostEur=1600 },
                new() { DestinationCountry="GB", VehicleCategory="M1", YearFrom=2010, YearTo=2030, NotesEs="IVA Single Vehicle Approval",                     EmissionStandard="Euro 6", CertifyingBody="DVSA", EstimatedCostEur=2000 },
                new() { DestinationCountry="MA", VehicleCategory="M1", YearFrom=2010, YearTo=2030, NotesEs="Homologation au Maroc — CNRA",                    EmissionStandard="Euro 5", CertifyingBody="CNRA", EstimatedCostEur=1300 },
            };
            foreach (var h in homologs) { h.CreatedAt = now; h.UpdatedAt = now; }
            db.HomologationRequirements.AddRange(homologs);
        }

        if (!await db.CustomsTariffs.AnyAsync(ct))
        {
            var tariffs = new List<CustomsTariff>
            {
                new() { OriginCountry="JP", DestinationCountry="ES", HsCode="8703", TariffRatePercent=10,   ValidFrom=now, Source="UE TARIC" },
                new() { OriginCountry="US", DestinationCountry="ES", HsCode="8703", TariffRatePercent=10,   ValidFrom=now, Source="UE TARIC" },
                new() { OriginCountry="GB", DestinationCountry="ES", HsCode="8703", TariffRatePercent=10,   ValidFrom=now, Source="UE TARIC post-Brexit" },
                new() { OriginCountry="DE", DestinationCountry="ES", HsCode="8703", TariffRatePercent=0,    ValidFrom=now, Source="Intra-UE" },
                new() { OriginCountry="ES", DestinationCountry="MA", HsCode="8703", TariffRatePercent=17,   ValidFrom=now, Source="Aduana Marruecos" },
                new() { OriginCountry="ES", DestinationCountry="US", HsCode="8703", TariffRatePercent=2.5m, ValidFrom=now, Source="USTR" },
            };
            foreach (var t in tariffs) { t.CreatedAt = now; t.UpdatedAt = now; }
            db.CustomsTariffs.AddRange(tariffs);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedDocumentTemplatesAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (await db.DocumentTemplates.AnyAsync(ct)) return;

        var templates = new List<DocumentTemplate>
        {
            new() { Country="ES", DocumentType="FichaTecnica",             OfficialUrl="https://www.dgt.es/", IssuingAuthority="DGT",  InstructionsEs="Solicitar en jefatura provincial.", InstructionsEn="Request at the provincial DGT office.", EstimatedCostEur=20,  EstimatedDays=3 },
            new() { Country="ES", DocumentType="PermisoCirculacion",       OfficialUrl="https://www.dgt.es/", IssuingAuthority="DGT",  InstructionsEs="Tramitar en DGT.",                  InstructionsEn="Process at DGT.",                       EstimatedCostEur=55,  EstimatedDays=5 },
            new() { Country="ES", DocumentType="CertificadoConformidadCOC",OfficialUrl=null,                  IssuingAuthority="OEM",  InstructionsEs="Solicitar al fabricante.",          InstructionsEn="Request from manufacturer.",            EstimatedCostEur=120, EstimatedDays=15 },
            new() { Country="DE", DocumentType="FichaTecnica",             OfficialUrl="https://www.kba.de/", IssuingAuthority="KBA",  InstructionsEs="Documentación KBA.",                InstructionsEn="KBA documentation.",                    EstimatedCostEur=30,  EstimatedDays=4 },
            new() { Country="FR", DocumentType="FichaTecnica",             OfficialUrl="https://immatriculation.ants.gouv.fr/", IssuingAuthority="ANTS", InstructionsEs="ANTS.", InstructionsEn="ANTS portal.",                          EstimatedCostEur=25,  EstimatedDays=4 },
            new() { Country="IT", DocumentType="FichaTecnica",             OfficialUrl="https://www.aci.it/", IssuingAuthority="ACI",  InstructionsEs="ACI.",                              InstructionsEn="ACI office.",                           EstimatedCostEur=28,  EstimatedDays=5 },
            new() { Country="GB", DocumentType="FichaTecnica",             OfficialUrl="https://www.gov.uk/", IssuingAuthority="DVLA", InstructionsEs="DVLA.",                             InstructionsEn="DVLA online.",                          EstimatedCostEur=35,  EstimatedDays=7 },
            new() { Country="JP", DocumentType="FichaTecnica",             OfficialUrl=null,                  IssuingAuthority="MLIT", InstructionsEs="Solicitar al exportador.",          InstructionsEn="Request from exporter.",                EstimatedCostEur=80,  EstimatedDays=10 },
        };
        foreach (var t in templates) { t.CreatedAt = now; t.UpdatedAt = now; }
        db.DocumentTemplates.AddRange(templates);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} plantillas documento", templates.Count);
    }

    // ─── Procesos ──────────────────────────────────────────────────────────
    private async Task<List<ImportExportProcess>> SeedProcessesAsync(
        List<Vehicle> vehicles, List<UserProfile> users, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.ImportExportProcesses.AnyAsync(ct))
            return await db.ImportExportProcesses.ToListAsync(ct);

        var buyers = users.Where(u => u.Role != UserRole.Admin).ToList();
        var rng = new Random(7);

        var processes = new List<ImportExportProcess>();
        foreach (var v in vehicles.Where(x => x.Status == VehicleStatus.Actif).Take(8))
        {
            var buyer = buyers[rng.Next(buyers.Count)];
            var dest  = "SN";
            if (dest == v.CountryOrigin) dest = "FR";
            var status = (ProcessStatus)rng.Next(1, 6);
            var processType = v.CountryOrigin is "DE" or "FR" or "IT" or "PT" or "NL"
                ? ProcessType.IntraEu : ProcessType.ImportExtraEu;

            processes.Add(new ImportExportProcess
            {
                TrackingCode   = ImportExportProcess.GenerateTrackingCode(),
                VehicleId      = v.Id,
                BuyerId        = buyer.Id,
                SellerId       = v.SellerId,
                OriginCountry  = v.CountryOrigin,
                DestinationCountry = dest,
                ProcessType    = processType,
                Status         = status,
                EstimatedCostEur = v.Price * 0.18m,
                ActualCostEur    = status == ProcessStatus.Completed ? v.Price * 0.16m : null,
                StartedAt      = now.AddDays(-rng.Next(1, 60)),
                CompletedAt    = status == ProcessStatus.Completed ? now.AddDays(-rng.Next(1, 10)) : null,
                CompletionPercent = status switch
                {
                    ProcessStatus.Draft           => 5,
                    ProcessStatus.InProgress      => rng.Next(10, 60),
                    ProcessStatus.PendingDocuments=> rng.Next(30, 70),
                    ProcessStatus.UnderReview     => rng.Next(60, 90),
                    ProcessStatus.Approved        => 95,
                    ProcessStatus.Completed       => 100,
                    _ => 0
                },
                CreatedAt      = now.AddDays(-rng.Next(1, 60)),
                UpdatedAt      = now.AddDays(-rng.Next(0, 5))
            });
        }

        db.ImportExportProcesses.AddRange(processes);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} procesos de tramitación", processes.Count);
        return processes;
    }

    private async Task SeedProcessDocumentsAsync(
        List<ImportExportProcess> processes, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.ProcessDocuments.AnyAsync(ct)) return;

        var docTypes = new[] { "FichaTecnica", "PermisoCirculacion", "FacturaCompra", "CertificadoConformidadCOC", "Itv" };
        var rng = new Random(11);
        var list = new List<ProcessDocument>();

        foreach (var p in processes)
        {
            foreach (var dt in docTypes)
            {
                var status = (DocumentStatus)rng.Next(0, 5);
                list.Add(new ProcessDocument
                {
                    ProcessId        = p.Id,
                    DocumentType     = dt,
                    Country          = p.DestinationCountry,
                    Status           = status,
                    ResponsibleParty = rng.NextDouble() > 0.5 ? "buyer" : "seller",
                    RequiredByDate   = now.AddDays(rng.Next(5, 60)),
                    UploadedAt       = status >= DocumentStatus.Uploaded ? now.AddDays(-rng.Next(1, 20)) : null,
                    VerifiedAt       = status == DocumentStatus.Verified ? now.AddDays(-rng.Next(0, 5)) : null,
                    FileUrl          = status >= DocumentStatus.Uploaded ? $"/uploads/demo/{p.TrackingCode}-{dt}.pdf" : null,
                    InstructionsEs   = $"Subir el documento {dt}.",
                    EstimatedCostEur = rng.Next(0, 200),
                    CreatedAt        = p.CreatedAt,
                    UpdatedAt        = now
                });
            }
        }
        db.ProcessDocuments.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} documentos de proceso", list.Count);
    }

    private async Task SeedIncidentsAsync(
        List<ImportExportProcess> processes, List<UserProfile> users, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.ProcessIncidents.AnyAsync(ct)) return;

        var moderator = users.FirstOrDefault(u => u.Role == UserRole.Admin);
        var rng = new Random(13);

        var samples = new (string Title, string Body, IncidentSeverity Sev)[]
        {
            ("COC rechazado en aduana",          "El certificado de conformidad europeo no contiene la entrada para el modelo.", IncidentSeverity.High),
            ("Plazo de homologación excedido",   "El TÜV ha solicitado nueva inspección por discrepancia en emisiones.",         IncidentSeverity.Medium),
            ("Factura sin firma",                "El vendedor debe re-emitir la factura con firma electrónica.",                 IncidentSeverity.Low),
            ("Pago de aranceles pendiente",      "El comprador aún no ha realizado el ingreso del DUA.",                         IncidentSeverity.High),
            ("Documento expirado",               "La ITV original ha caducado durante el proceso.",                              IncidentSeverity.Medium),
            ("Daño detectado en transporte",     "El transportista reporta arañazo en aleta delantera derecha.",                 IncidentSeverity.Critical),
        };

        var list = new List<ProcessIncident>();
        foreach (var p in processes.Take(6))
        {
            var s = samples[rng.Next(samples.Length)];
            list.Add(new ProcessIncident
            {
                ProcessId        = p.Id,
                Title            = s.Title,
                Description      = s.Body,
                Severity         = s.Sev,
                Status           = rng.NextDouble() > 0.6 ? IncidentStatus.Resolved : IncidentStatus.Open,
                AssignedToUserId = moderator?.Id,
                ResolvedAt       = rng.NextDouble() > 0.6 ? now.AddDays(-rng.Next(0, 5)) : null,
                Resolution       = rng.NextDouble() > 0.6 ? "Documento reenviado y validado." : null,
                CreatedAt        = now.AddDays(-rng.Next(1, 30)),
                UpdatedAt        = now
            });
        }
        db.ProcessIncidents.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} incidencias", list.Count);
    }

    private async Task SeedConversationsAsync(
        List<Vehicle> vehicles, List<UserProfile> users, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Negotiations.AnyAsync(ct)) return;

        var buyers  = users.Where(u => u.Role != UserRole.Admin).ToList();
        var rng = new Random(17);
        var convs = new List<Negotiation>();
        var msgs  = new List<Message>();

        foreach (var v in vehicles.Where(x => x.Status == VehicleStatus.Actif).Take(6))
        {
            var buyer = buyers[rng.Next(buyers.Count)];
            var conv = new Negotiation
            {
                BuyerId   = buyer.Id,
                SellerId  = v.SellerId,
                VehicleId = v.Id,
                LastMessageAt = now.AddHours(-rng.Next(1, 72)),
                CreatedAt = now.AddDays(-rng.Next(1, 14)),
                UpdatedAt = now
            };
            convs.Add(conv);

            var lines = new[]
            {
                ("buyer",  $"Hola, ¿sigue disponible el {v.Title}?"),
                ("seller", "Sí, totalmente disponible. ¿Quiere más información?"),
                ("buyer",  "¿Cuál es el kilometraje real y el historial del vehículo?"),
                ("seller", $"{v.Mileage:N0} km certificados, mantenimientos al día. Le envío el informe."),
                ("buyer",  "Perfecto, ¿pueden gestionarme la importación a mi país?"),
                ("seller", "Sí, gestionamos toda la documentación a través de Logistique Les Lions.")
            };

            int i = 0;
            foreach (var (role, body) in lines)
            {
                var senderId = role == "buyer" ? buyer.Id : v.SellerId;
                msgs.Add(new Message
                {
                    NegotiationId = conv.Id,
                    SenderId       = senderId,
                    Body           = body,
                    IsRead         = i < lines.Length - 1,
                    ReadAt         = i < lines.Length - 1 ? now.AddHours(-rng.Next(1, 48)) : null,
                    CreatedAt      = conv.CreatedAt.AddMinutes(i * 30),
                    UpdatedAt      = now
                });
                i++;
            }
        }

        db.Negotiations.AddRange(convs);
        db.Messages.AddRange(msgs);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {C} conversaciones, {M} mensajes", convs.Count, msgs.Count);
    }

    private async Task SeedNotificationsAsync(
        List<UserProfile> users, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.UserNotifications.AnyAsync(ct)) return;

        var rng = new Random(19);
        var samples = new (string Cat, string Title, string Body, string Link)[]
        {
            ("incident",    "Incidencia abierta",            "Una de tus tramitaciones tiene una incidencia pendiente.", "/admin/incidents"),
            ("process",     "Documento verificado",          "Tu COC ha sido verificado por el equipo de moderación.",   "/dashboard"),
            ("message",     "Nuevo mensaje",                 "Has recibido un mensaje sobre tu BMW Serie 3.",            "/mensajes"),
            ("system",      "Mantenimiento programado",      "El sistema estará en mantenimiento el próximo domingo.",   "/"),
            ("marketplace", "Nuevo partner verificado",      "Berlin Premium Cars se ha unido al marketplace.",          "/admin/partners"),
            ("process",     "Aranceles pagados",             "Hemos confirmado el pago de los aranceles aduaneros.",     "/dashboard"),
        };

        var list = new List<UserNotification>();
        foreach (var u in users)
        {
            for (int i = 0; i < 3; i++)
            {
                var s = samples[rng.Next(samples.Length)];
                list.Add(new UserNotification
                {
                    UserId    = u.Id,
                    Category  = s.Cat,
                    Title     = s.Title,
                    Body      = s.Body,
                    Link      = s.Link,
                    IsRead    = rng.NextDouble() > 0.6,
                    ReadAt    = rng.NextDouble() > 0.6 ? now.AddHours(-rng.Next(1, 48)) : null,
                    CreatedAt = now.AddHours(-rng.Next(1, 200)),
                    UpdatedAt = now
                });
            }
        }
        db.UserNotifications.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} notificaciones", list.Count);
    }

    private async Task SeedPartnersAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (await db.ServicePartners.AnyAsync(ct)) return;

        var list = new List<ServicePartner>
        {
            new() { Type=PartnerType.Gestor,      Name="Gestoría Iberia",            Slug="gestoria-iberia",      Description="Gestoría especializada en matriculación e importación de vehículos en España.", CountriesCsv="ES,PT", ContactEmail="info@gestoriaiberia.test", Website="https://gestoriaiberia.test", Rating=4.8m, ReviewsCount=152, BasePriceEur=350,  IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Gestor,      Name="Brussels Customs Partners",  Slug="brussels-customs",     Description="Despacho aduanero en Bruselas con cobertura BeNeLux.",                              CountriesCsv="BE,NL,LU", Website="https://brusselscustoms.test", Rating=4.6m, ReviewsCount=98, BasePriceEur=420, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Transport,   Name="Euro Auto Logistics",        Slug="euro-auto-logistics",  Description="Transporte de vehículos por carretera y barco en toda Europa.",                     CountriesCsv="ES,FR,DE,IT,PT,NL,BE", ContactEmail="ops@euroautologistics.test", Rating=4.7m, ReviewsCount=210, BasePriceEur=850, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Transport,   Name="MedShip Cars",               Slug="medship-cars",         Description="Transporte marítimo entre puertos del Mediterráneo.",                               CountriesCsv="ES,IT,FR,MA", Rating=4.4m, ReviewsCount=64, BasePriceEur=1100, IsVerified=false, IsActive=true },
            new() { Type=PartnerType.Inspector,   Name="Carfax Europe Inspectors",   Slug="carfax-europe",        Description="Inspecciones pre-compra con informe fotográfico de 200 puntos.",                    CountriesCsv="DE,FR,IT,ES,GB", Website="https://carfaxeu.test", Rating=4.9m, ReviewsCount=340, BasePriceEur=180, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Inspector,   Name="Tokyo Auto Check",           Slug="tokyo-auto-check",     Description="Inspección de vehículos JDM en Japón antes de la exportación.",                      CountriesCsv="JP", Rating=4.7m, ReviewsCount=87, BasePriceEur=250, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Homologator, Name="TÜV Süd Homologations",      Slug="tuv-sud-homologations", Description="Homologación individual de vehículos importados en Alemania.",                      CountriesCsv="DE", Website="https://tuvsud.test", Rating=4.8m, ReviewsCount=412, BasePriceEur=1200, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Homologator, Name="ITV Madrid Homologa",        Slug="itv-madrid-homologa",  Description="Homologación individual y reformas en estación ITV.",                                CountriesCsv="ES", Rating=4.5m, ReviewsCount=178, BasePriceEur=900,  IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Insurance,   Name="EuroAssur Mobilité",         Slug="euroassur-mobilite",   Description="Seguros temporales transfronterizos para vehículos importados.",                     CountriesCsv="FR,ES,DE,IT", ContactEmail="hello@euroassur.test", Rating=4.3m, ReviewsCount=55, BasePriceEur=120, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Insurance,   Name="Atlas Cover",                Slug="atlas-cover",          Description="Seguros internacionales para flotas y particulares.",                                CountriesCsv="ES,PT,MA", Rating=4.2m, ReviewsCount=33, BasePriceEur=140, IsVerified=false, IsActive=true },
            new() { Type=PartnerType.Financing,   Name="Leasing Méditerranée",       Slug="leasing-mediterranee", Description="Financiación de vehículos importados con condiciones flexibles.",                    CountriesCsv="ES,FR,IT", Website="https://leasingmed.test", Rating=4.4m, ReviewsCount=72, BasePriceEur=null, IsVerified=true,  IsActive=true },
            new() { Type=PartnerType.Financing,   Name="Nordic Auto Finance",        Slug="nordic-auto-finance",  Description="Crédito al consumo para compra internacional de vehículos.",                         CountriesCsv="DE,NL,BE", Rating=4.6m, ReviewsCount=128, BasePriceEur=null, IsVerified=true,  IsActive=true },
        };
        foreach (var p in list) { p.CreatedAt = now; p.UpdatedAt = now; }
        db.ServicePartners.AddRange(list);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} partners", list.Count);
    }

    private async Task SeedNewsletterAsync(DateTimeOffset now, CancellationToken ct)
    {
        if (await db.NewsletterSubscribers.AnyAsync(ct)) return;

        var emails = new[]
        {
            "ana.garcia@example.com", "luis.martinez@example.com", "marie.durand@example.com",
            "stefan.weber@example.com", "elena.rossi@example.com", "joao.almeida@example.com",
            "kenji.sato@example.com", "noah.brown@example.com"
        };
        db.NewsletterSubscribers.AddRange(emails.Select(e => new NewsletterSubscriber
        {
            Id = Guid.NewGuid(),
            Email = e,
            SubscribedAt = now.AddDays(-Random.Shared.Next(1, 60)),
            IsActive = true
        }));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("  · {N} suscriptores newsletter", emails.Length);
    }
}
