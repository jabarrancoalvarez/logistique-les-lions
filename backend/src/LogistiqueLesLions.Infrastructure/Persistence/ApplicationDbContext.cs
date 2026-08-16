using System.Text.Json;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LogistiqueLesLions.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de EF Core. Implementa IApplicationDbContext para
/// desacoplar la capa de Application de los detalles de infraestructura.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    AuditInterceptor auditInterceptor,
    AuditLogInterceptor auditLogInterceptor)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ─── M1 ────────────────────────────────────────────────────────────────
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<VehicleEquipment> VehicleEquipments => Set<VehicleEquipment>();
    public DbSet<VehicleEquipmentLink> VehicleEquipmentLinks => Set<VehicleEquipmentLink>();
    public DbSet<VehiclePriceHistory> VehiclePriceHistories => Set<VehiclePriceHistory>();
    public DbSet<PriceIndicatorSettings> PriceIndicatorSettings => Set<PriceIndicatorSettings>();

    // ─── M2 ────────────────────────────────────────────────────────────────
    public DbSet<VehicleHistory> VehicleHistories => Set<VehicleHistory>();
    public DbSet<SavedVehicle> SavedVehicles => Set<SavedVehicle>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<VehicleRequest> VehicleRequests => Set<VehicleRequest>();
    public DbSet<VehicleRequestMessage> VehicleRequestMessages => Set<VehicleRequestMessage>();
    public DbSet<VehicleRequestProposal> VehicleRequestProposals => Set<VehicleRequestProposal>();

    // ─── M3 ────────────────────────────────────────────────────────────────

    // ─── M5 ────────────────────────────────────────────────────────────────
    public DbSet<Negotiation> Negotiations => Set<Negotiation>();
    public DbSet<NegotiationEvent> NegotiationEvents => Set<NegotiationEvent>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<VehicleInspection> VehicleInspections => Set<VehicleInspection>();
    public DbSet<VehicleInspectionItem> VehicleInspectionItems => Set<VehicleInspectionItem>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    // ─── Mon Garage ────────────────────────────────────────────────────────
    public DbSet<GarageVehicle> GarageVehicles => Set<GarageVehicle>();
    public DbSet<GarageVehicleImage> GarageVehicleImages => Set<GarageVehicleImage>();
    public DbSet<GarageDocument> GarageDocuments => Set<GarageDocument>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<MaintenanceRecordImage> MaintenanceRecordImages => Set<MaintenanceRecordImage>();
    public DbSet<VehicleReminder> VehicleReminders => Set<VehicleReminder>();
    public DbSet<VehicleValuationSnapshot> VehicleValuationSnapshots => Set<VehicleValuationSnapshot>();
    public DbSet<VehicleValuationSettings> VehicleValuationSettings => Set<VehicleValuationSettings>();
    public DbSet<VehicleTransparency> VehicleTransparencies => Set<VehicleTransparency>();
    public DbSet<SharedMaintenanceRecord> SharedMaintenanceRecords => Set<SharedMaintenanceRecord>();

    // ─── M6 ────────────────────────────────────────────────────────────────
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<AdminNote> AdminNotes => Set<AdminNote>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Communication> Communications => Set<Communication>();

    public DbSet<LoyaltyPointEntry> LoyaltyPointEntries => Set<LoyaltyPointEntry>();

    // ─── Configuration ──────────────────────────────────────────────────────
    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<UpcomingFeature> UpcomingFeatures => Set<UpcomingFeature>();
    public DbSet<FeatureInterest> FeatureInterests => Set<FeatureInterest>();

    // ─── Newsletter ─────────────────────────────────────────────────────────

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Orden importante: AuditInterceptor primero (rellena timestamps),
        // AuditLogInterceptor después (lee los valores ya enriquecidos).
        optionsBuilder.AddInterceptors(auditInterceptor, auditLogInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Crear schemas de PostgreSQL separados por módulo
        modelBuilder.HasDefaultSchema("vehicles");

        // Registrar todas las configuraciones del assembly automáticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Conversor genérico JsonDocument? <-> string para que el provider InMemory
        // (usado en tests) pueda mapear las propiedades jsonb. En PostgreSQL la
        // columna sigue siendo jsonb (definida en la configuración de cada entidad).
        var jsonDocConverter = new ValueConverter<JsonDocument?, string?>(
            v => v == null ? null : v.RootElement.GetRawText(),
            v => string.IsNullOrEmpty(v) ? null : JsonDocument.Parse(v, default));
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entityType.GetProperties())
            {
                if (prop.ClrType == typeof(JsonDocument) || prop.ClrType == typeof(JsonDocument))
                    prop.SetValueConverter(jsonDocConverter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Override para disparar eventos de dominio después de persistir.
    /// Los eventos se publican vía MediatR (Outbox pattern en producción).
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
