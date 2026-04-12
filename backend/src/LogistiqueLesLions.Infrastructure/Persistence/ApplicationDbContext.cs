using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de EF Core. Implementa IApplicationDbContext para
/// desacoplar la capa de Application de los detalles de infraestructura.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    AuditInterceptor auditInterceptor)
    : DbContext(options), IApplicationDbContext
{
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

    // ─── M5 ────────────────────────────────────────────────────────────────
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    // ─── M6 ────────────────────────────────────────────────────────────────
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // ─── Newsletter ─────────────────────────────────────────────────────────
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Crear schemas de PostgreSQL separados por módulo
        modelBuilder.HasDefaultSchema("vehicles");

        // Registrar todas las configuraciones del assembly automáticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

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
