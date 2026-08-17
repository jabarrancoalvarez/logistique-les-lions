class AdminDashboard {
  final AdminUserStats users;
  final AdminMarketplaceStats marketplace;
  final AdminActivityStats activity;
  final AdminDemandStats demand;
  final AdminGarageStats garage;

  const AdminDashboard({
    required this.users,
    required this.marketplace,
    required this.activity,
    required this.demand,
    required this.garage,
  });

  factory AdminDashboard.fromJson(Map<String, dynamic> j) => AdminDashboard(
        users: AdminUserStats.fromJson(j['users'] as Map<String, dynamic>),
        marketplace: AdminMarketplaceStats.fromJson(
            j['marketplace'] as Map<String, dynamic>),
        activity:
            AdminActivityStats.fromJson(j['activity'] as Map<String, dynamic>),
        demand: AdminDemandStats.fromJson(j['demand'] as Map<String, dynamic>),
        garage: AdminGarageStats.fromJson(j['garage'] as Map<String, dynamic>),
      );
}

class AdminUserStats {
  final int total, newToday, newLast7Days, newLast30Days;
  final int particuliers, professionnels, phoneVerified;
  const AdminUserStats(this.total, this.newToday, this.newLast7Days,
      this.newLast30Days, this.particuliers, this.professionnels,
      this.phoneVerified);
  factory AdminUserStats.fromJson(Map<String, dynamic> j) => AdminUserStats(
        j['total'] as int? ?? 0,
        j['newToday'] as int? ?? 0,
        j['newLast7Days'] as int? ?? 0,
        j['newLast30Days'] as int? ?? 0,
        j['particuliers'] as int? ?? 0,
        j['professionnels'] as int? ?? 0,
        j['phoneVerified'] as int? ?? 0,
      );
}

class AdminMarketplaceStats {
  final int active, newLast7Days, reserved, sold, drafts, paused, archived;
  final int pendingModeration;
  const AdminMarketplaceStats(this.active, this.newLast7Days, this.reserved,
      this.sold, this.drafts, this.paused, this.archived, this.pendingModeration);
  factory AdminMarketplaceStats.fromJson(Map<String, dynamic> j) =>
      AdminMarketplaceStats(
        j['active'] as int? ?? 0,
        j['newLast7Days'] as int? ?? 0,
        j['reserved'] as int? ?? 0,
        j['sold'] as int? ?? 0,
        j['drafts'] as int? ?? 0,
        j['paused'] as int? ?? 0,
        j['archived'] as int? ?? 0,
        j['pendingModeration'] as int? ?? 0,
      );
}

class AdminActivityStats {
  final int negotiationsStarted, negotiationsActive, messagesSent, offersMade;
  final int offersAccepted, contractsCreated, contractsValidated, verifiedSales;
  const AdminActivityStats(this.negotiationsStarted, this.negotiationsActive,
      this.messagesSent, this.offersMade, this.offersAccepted,
      this.contractsCreated, this.contractsValidated, this.verifiedSales);
  factory AdminActivityStats.fromJson(Map<String, dynamic> j) =>
      AdminActivityStats(
        j['negotiationsStarted'] as int? ?? 0,
        j['negotiationsActive'] as int? ?? 0,
        j['messagesSent'] as int? ?? 0,
        j['offersMade'] as int? ?? 0,
        j['offersAccepted'] as int? ?? 0,
        j['contractsCreated'] as int? ?? 0,
        j['contractsValidated'] as int? ?? 0,
        j['verifiedSales'] as int? ?? 0,
      );
}

class AdminDemandStats {
  final int savedSearches, favoritesTotal, requestsPending, requestsSearching;
  const AdminDemandStats(this.savedSearches, this.favoritesTotal,
      this.requestsPending, this.requestsSearching);
  factory AdminDemandStats.fromJson(Map<String, dynamic> j) => AdminDemandStats(
        j['savedSearches'] as int? ?? 0,
        j['favoritesTotal'] as int? ?? 0,
        j['requestsPending'] as int? ?? 0,
        j['requestsSearching'] as int? ?? 0,
      );
}

class AdminGarageStats {
  final int vehiclesTotal, fromYoonUAuto, addedManually, convertedToListings;
  const AdminGarageStats(this.vehiclesTotal, this.fromYoonUAuto,
      this.addedManually, this.convertedToListings);
  factory AdminGarageStats.fromJson(Map<String, dynamic> j) => AdminGarageStats(
        j['vehiclesTotal'] as int? ?? 0,
        j['fromYoonUAuto'] as int? ?? 0,
        j['addedManually'] as int? ?? 0,
        j['convertedToListings'] as int? ?? 0,
      );
}
