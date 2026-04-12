using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Abstracción del DbContext para desacoplar Application de Infrastructure.
/// Sólo expone los DbSets necesarios para la capa de aplicación.
/// </summary>
public interface IApplicationDbContext
{
    // ─── M1: Catálogo base ─────────────────────────────────────────────────
    DbSet<Vehicle> Vehicles { get; }
    DbSet<VehicleMake> VehicleMakes { get; }
    DbSet<VehicleModel> VehicleModels { get; }
    DbSet<VehicleImage> VehicleImages { get; }
    DbSet<Country> Countries { get; }

    // ─── M2: Vehículos ampliado ────────────────────────────────────────────
    DbSet<VehicleDocument> VehicleDocuments { get; }
    DbSet<VehicleHistory> VehicleHistories { get; }
    DbSet<SavedVehicle> SavedVehicles { get; }

    // ─── M3: Tramitación / Compliance ──────────────────────────────────────
    DbSet<CountryRequirement> CountryRequirements { get; }
    DbSet<ImportExportProcess> ImportExportProcesses { get; }
    DbSet<ProcessDocument> ProcessDocuments { get; }
    DbSet<DocumentTemplate> DocumentTemplates { get; }
    DbSet<HomologationRequirement> HomologationRequirements { get; }
    DbSet<CustomsTariff> CustomsTariffs { get; }
    DbSet<ProcessIncident> ProcessIncidents { get; }

    // ─── M5: Mensajería ────────────────────────────────────────────────────
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<UserNotification> UserNotifications { get; }
    DbSet<ServicePartner> ServicePartners { get; }

    // ─── M6: Usuarios ──────────────────────────────────────────────────────
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<NewsletterSubscriber> NewsletterSubscribers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
