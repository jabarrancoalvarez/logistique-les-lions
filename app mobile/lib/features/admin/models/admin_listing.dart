import 'admin_common.dart';

class AdminListingRow {
  final String id;
  final String publicReference;
  final String slug;
  final String title;
  final String status;
  final bool hiddenByAdmin;
  final bool flaggedForReview;
  final num price;
  final String? city;
  final String sellerId;
  final String sellerName;
  final String sellerAccountType;
  final int viewsCount;
  final int favoritesCount;
  final int qualityScore;
  final int openReports;
  final DateTime createdAt;

  const AdminListingRow({
    required this.id,
    required this.publicReference,
    required this.slug,
    required this.title,
    required this.status,
    required this.hiddenByAdmin,
    required this.flaggedForReview,
    required this.price,
    required this.sellerId,
    required this.sellerName,
    required this.sellerAccountType,
    required this.viewsCount,
    required this.favoritesCount,
    required this.qualityScore,
    required this.openReports,
    required this.createdAt,
    this.city,
  });

  factory AdminListingRow.fromJson(Map<String, dynamic> j) => AdminListingRow(
        id: j['id'] as String,
        publicReference: (j['publicReference'] ?? '') as String,
        slug: (j['slug'] ?? '') as String,
        title: (j['title'] ?? '') as String,
        status: (j['status'] ?? 'Actif') as String,
        hiddenByAdmin: j['hiddenByAdmin'] as bool? ?? false,
        flaggedForReview: j['flaggedForReview'] as bool? ?? false,
        price: (j['price'] as num?) ?? 0,
        city: j['city'] as String?,
        sellerId: (j['sellerId'] ?? '') as String,
        sellerName: (j['sellerName'] ?? '') as String,
        sellerAccountType: (j['sellerAccountType'] ?? 'Particulier') as String,
        viewsCount: j['viewsCount'] as int? ?? 0,
        favoritesCount: j['favoritesCount'] as int? ?? 0,
        qualityScore: j['qualityScore'] as int? ?? 0,
        openReports: j['openReports'] as int? ?? 0,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}

class AdminListingDetail {
  final AdminListingRow listing;
  final String sellerPhone;
  final int contactsCount;
  final int negotiationsCount;
  final int offersReceived;
  final List<AdminAction> actions;
  final List<AdminNote> notes;

  const AdminListingDetail({
    required this.listing,
    required this.sellerPhone,
    required this.contactsCount,
    required this.negotiationsCount,
    required this.offersReceived,
    required this.actions,
    required this.notes,
  });

  factory AdminListingDetail.fromJson(Map<String, dynamic> j) =>
      AdminListingDetail(
        listing: AdminListingRow.fromJson(j['listing'] as Map<String, dynamic>),
        sellerPhone: (j['sellerPhone'] ?? '') as String,
        contactsCount: j['contactsCount'] as int? ?? 0,
        negotiationsCount: j['negotiationsCount'] as int? ?? 0,
        offersReceived: j['offersReceived'] as int? ?? 0,
        actions: (j['actions'] as List<dynamic>? ?? const [])
            .map((e) => AdminAction.fromJson(e as Map<String, dynamic>))
            .toList(),
        notes: (j['notes'] as List<dynamic>? ?? const [])
            .map((e) => AdminNote.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
