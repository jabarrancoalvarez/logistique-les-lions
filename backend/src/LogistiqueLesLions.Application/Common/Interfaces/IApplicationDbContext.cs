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
    DbSet<VehicleEquipment> VehicleEquipments { get; }
    DbSet<VehicleEquipmentLink> VehicleEquipmentLinks { get; }
    DbSet<VehiclePriceHistory> VehiclePriceHistories { get; }
    DbSet<PriceIndicatorSettings> PriceIndicatorSettings { get; }
    DbSet<Country> Countries { get; }

    // ─── M2: Vehículos ampliado ────────────────────────────────────────────
    DbSet<VehicleDocument> VehicleDocuments { get; }
    DbSet<VehicleHistory> VehicleHistories { get; }
    DbSet<SavedVehicle> SavedVehicles { get; }
    DbSet<SavedSearch> SavedSearches { get; }
    DbSet<VehicleRequest> VehicleRequests { get; }
    DbSet<VehicleRequestMessage> VehicleRequestMessages { get; }
    DbSet<VehicleRequestProposal> VehicleRequestProposals { get; }

    // ─── M3: Tramitación / Compliance ──────────────────────────────────────
    DbSet<CountryRequirement> CountryRequirements { get; }
    DbSet<ImportExportProcess> ImportExportProcesses { get; }
    DbSet<ProcessDocument> ProcessDocuments { get; }
    DbSet<DocumentTemplate> DocumentTemplates { get; }
    DbSet<HomologationRequirement> HomologationRequirements { get; }
    DbSet<CustomsTariff> CustomsTariffs { get; }
    DbSet<ProcessIncident> ProcessIncidents { get; }

    // ─── M5: Mensajería ────────────────────────────────────────────────────
    DbSet<Negotiation> Negotiations { get; }
    DbSet<NegotiationEvent> NegotiationEvents { get; }
    DbSet<Offer> Offers { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<VehicleInspection> VehicleInspections { get; }
    DbSet<VehicleInspectionItem> VehicleInspectionItems { get; }
    DbSet<Message> Messages { get; }
    DbSet<UserNotification> UserNotifications { get; }
    DbSet<ServicePartner> ServicePartners { get; }

    // ─── Mon Garage ────────────────────────────────────────────────────────
    DbSet<GarageVehicle> GarageVehicles { get; }
    DbSet<GarageVehicleImage> GarageVehicleImages { get; }
    DbSet<GarageDocument> GarageDocuments { get; }
    DbSet<MaintenanceRecord> MaintenanceRecords { get; }
    DbSet<MaintenanceRecordImage> MaintenanceRecordImages { get; }
    DbSet<VehicleReminder> VehicleReminders { get; }
    DbSet<VehicleValuationSnapshot> VehicleValuationSnapshots { get; }
    DbSet<VehicleValuationSettings> VehicleValuationSettings { get; }
    DbSet<VehicleTransparency> VehicleTransparencies { get; }
    DbSet<SharedMaintenanceRecord> SharedMaintenanceRecords { get; }

    // ─── M6: Usuarios ──────────────────────────────────────────────────────
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<AdminAction> AdminActions { get; }
    DbSet<AdminNote> AdminNotes { get; }
    DbSet<Report> Reports { get; }
    DbSet<Communication> Communications { get; }
    DbSet<LoyaltyPointEntry> LoyaltyPointEntries { get; }

    // ─── Configuration ──────────────────────────────────────────────────────
    DbSet<PlatformSettings> PlatformSettings { get; }
    DbSet<FeatureFlag> FeatureFlags { get; }
    DbSet<UpcomingFeature> UpcomingFeatures { get; }
    DbSet<FeatureInterest> FeatureInterests { get; }

    DbSet<NewsletterSubscriber> NewsletterSubscribers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
