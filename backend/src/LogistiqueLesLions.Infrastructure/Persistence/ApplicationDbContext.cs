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
    public DbSet<Country> Countries => Set<Country>();

    // ─── M2 ────────────────────────────────────────────────────────────────
    public DbSet<VehicleDocument> VehicleDocuments => Set<VehicleDocument>();
    public DbSet<VehicleHistory> VehicleHistories => Set<VehicleHistory>();
    public DbSet<SavedVehicle> SavedVehicles => Set<SavedVehicle>();

    // ─── M3 ────────────────────────────────────────────────────────────────
    public DbSet<CountryRequirement> CountryRequirements => Set<CountryRequirement>();
    public DbSet<ImportExportProcess> ImportExportProcesses => Set<ImportExportProcess>();
    public DbSet<ProcessDocument> ProcessDocuments => Set<ProcessDocument>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<HomologationRequirement> HomologationRequirements => Set<HomologationRequirement>();
    public DbSet<CustomsTariff> CustomsTariffs => Set<CustomsTariff>();
    public DbSet<ProcessIncident> ProcessIncidents => Set<ProcessIncident>();

    // ─── M5 ────────────────────────────────────────────────────────────────
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<ServicePartner> ServicePartners => Set<ServicePartner>();

    // ─── M6 ────────────────────────────────────────────────────────────────
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // ─── Newsletter ─────────────────────────────────────────────────────────
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

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
