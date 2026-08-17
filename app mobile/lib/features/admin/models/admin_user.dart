import 'admin_common.dart';

class AdminUserRow {
  final String id;
  final String displayName;
  final String phone;
  final bool phoneVerified;
  final String? email;
  final String? city;
  final String accountType;
  final String status;
  final DateTime? suspendedUntil;
  final String role;
  final int listingsCount;
  final int verifiedSalesCount;
  final DateTime createdAt;
  final DateTime? lastLoginAt;

  const AdminUserRow({
    required this.id,
    required this.displayName,
    required this.phone,
    required this.phoneVerified,
    required this.accountType,
    required this.status,
    required this.role,
    required this.listingsCount,
    required this.verifiedSalesCount,
    required this.createdAt,
    this.email,
    this.city,
    this.suspendedUntil,
    this.lastLoginAt,
  });

  factory AdminUserRow.fromJson(Map<String, dynamic> j) => AdminUserRow(
        id: j['id'] as String,
        displayName: (j['displayName'] ?? '') as String,
        phone: (j['phone'] ?? '') as String,
        phoneVerified: j['phoneVerified'] as bool? ?? false,
        email: j['email'] as String?,
        city: j['city'] as String?,
        accountType: (j['accountType'] ?? 'Particulier') as String,
        status: (j['status'] ?? 'Active') as String,
        suspendedUntil:
            DateTime.tryParse((j['suspendedUntil'] ?? '') as String),
        role: (j['role'] ?? 'User') as String,
        listingsCount: j['listingsCount'] as int? ?? 0,
        verifiedSalesCount: j['verifiedSalesCount'] as int? ?? 0,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
        lastLoginAt: DateTime.tryParse((j['lastLoginAt'] ?? '') as String),
      );
}

class AdminUserDetail {
  final AdminUserRow profile;
  final String? region;
  final AdminUserActivity activity;
  final List<AdminAction> actions;
  final List<AdminNote> notes;

  const AdminUserDetail({
    required this.profile,
    required this.activity,
    required this.actions,
    required this.notes,
    this.region,
  });

  factory AdminUserDetail.fromJson(Map<String, dynamic> j) => AdminUserDetail(
        profile: AdminUserRow.fromJson(j['profile'] as Map<String, dynamic>),
        region: j['region'] as String?,
        activity:
            AdminUserActivity.fromJson(j['activity'] as Map<String, dynamic>),
        actions: (j['actions'] as List<dynamic>? ?? const [])
            .map((e) => AdminAction.fromJson(e as Map<String, dynamic>))
            .toList(),
        notes: (j['notes'] as List<dynamic>? ?? const [])
            .map((e) => AdminNote.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class AdminUserActivity {
  final int listingsPublished, listingsSold, negotiations, offersMade;
  final int contracts, verifiedSales, requests, garageVehicles;
  final int reportsReceived, reportsMade;

  const AdminUserActivity({
    required this.listingsPublished,
    required this.listingsSold,
    required this.negotiations,
    required this.offersMade,
    required this.contracts,
    required this.verifiedSales,
    required this.requests,
    required this.garageVehicles,
    required this.reportsReceived,
    required this.reportsMade,
  });

  factory AdminUserActivity.fromJson(Map<String, dynamic> j) =>
      AdminUserActivity(
        listingsPublished: j['listingsPublished'] as int? ?? 0,
        listingsSold: j['listingsSold'] as int? ?? 0,
        negotiations: j['negotiations'] as int? ?? 0,
        offersMade: j['offersMade'] as int? ?? 0,
        contracts: j['contracts'] as int? ?? 0,
        verifiedSales: j['verifiedSales'] as int? ?? 0,
        requests: j['requests'] as int? ?? 0,
        garageVehicles: j['garageVehicles'] as int? ?? 0,
        reportsReceived: j['reportsReceived'] as int? ?? 0,
        reportsMade: j['reportsMade'] as int? ?? 0,
      );
}
