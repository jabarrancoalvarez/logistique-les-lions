/// Detalle de una negociación (`NegotiationDetailDto`): anuncio, partes y cronología.
class NegotiationDetail {
  final String id;
  final String status;
  final bool isBuyer;

  final String vehicleId;
  final String vehicleSlug;
  final String vehicleTitle;
  final String vehiclePublicReference;
  final num vehiclePrice;
  final String vehicleStatus;
  final String? vehicleThumbnailUrl;
  final bool acceptsNegotiation;

  final String otherUserId;
  final String otherUserName;
  final String? otherUserAvatarUrl;

  final DateTime createdAt;
  final DateTime? lastActivityAt;

  final List<NegotiationEventItem> timeline;
  final List<Offer> offers;
  final Offer? pendingOffer;

  const NegotiationDetail({
    required this.id,
    required this.status,
    required this.isBuyer,
    required this.vehicleId,
    required this.vehicleSlug,
    required this.vehicleTitle,
    required this.vehiclePublicReference,
    required this.vehiclePrice,
    required this.vehicleStatus,
    required this.acceptsNegotiation,
    required this.otherUserId,
    required this.otherUserName,
    required this.createdAt,
    required this.timeline,
    required this.offers,
    this.vehicleThumbnailUrl,
    this.otherUserAvatarUrl,
    this.lastActivityAt,
    this.pendingOffer,
  });

  factory NegotiationDetail.fromJson(Map<String, dynamic> j) => NegotiationDetail(
        id: j['id'] as String,
        status: (j['status'] ?? 'EnCours') as String,
        isBuyer: j['isBuyer'] as bool? ?? false,
        vehicleId: (j['vehicleId'] ?? '') as String,
        vehicleSlug: (j['vehicleSlug'] ?? '') as String,
        vehicleTitle: (j['vehicleTitle'] ?? '') as String,
        vehiclePublicReference: (j['vehiclePublicReference'] ?? '') as String,
        vehiclePrice: (j['vehiclePrice'] as num?) ?? 0,
        vehicleStatus: (j['vehicleStatus'] ?? 'Actif') as String,
        vehicleThumbnailUrl: j['vehicleThumbnailUrl'] as String?,
        acceptsNegotiation: j['acceptsNegotiation'] as bool? ?? false,
        otherUserId: (j['otherUserId'] ?? '') as String,
        otherUserName: (j['otherUserName'] ?? '') as String,
        otherUserAvatarUrl: j['otherUserAvatarUrl'] as String?,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
        lastActivityAt:
            DateTime.tryParse((j['lastActivityAt'] ?? '') as String),
        timeline: (j['timeline'] as List<dynamic>? ?? const [])
            .map((e) => NegotiationEventItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        offers: (j['offers'] as List<dynamic>? ?? const [])
            .map((e) => Offer.fromJson(e as Map<String, dynamic>))
            .toList(),
        pendingOffer: j['pendingOffer'] == null
            ? null
            : Offer.fromJson(j['pendingOffer'] as Map<String, dynamic>),
      );
}

class Offer {
  final String id;
  final num amount;
  final num listedPrice;
  final String? message;
  final String status;
  final bool byMe;
  final bool canRespond;
  final DateTime createdAt;
  final DateTime? respondedAt;

  const Offer({
    required this.id,
    required this.amount,
    required this.listedPrice,
    required this.status,
    required this.byMe,
    required this.canRespond,
    required this.createdAt,
    this.message,
    this.respondedAt,
  });

  factory Offer.fromJson(Map<String, dynamic> j) => Offer(
        id: j['id'] as String,
        amount: (j['amount'] as num?) ?? 0,
        listedPrice: (j['listedPrice'] as num?) ?? 0,
        message: j['message'] as String?,
        status: (j['status'] ?? 'EnAttente') as String,
        byMe: j['byMe'] as bool? ?? false,
        canRespond: j['canRespond'] as bool? ?? false,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
        respondedAt: DateTime.tryParse((j['respondedAt'] ?? '') as String),
      );
}

class NegotiationEventItem {
  final String id;
  final String type;
  final num? amount;
  final bool byMe;
  final DateTime createdAt;

  const NegotiationEventItem({
    required this.id,
    required this.type,
    required this.byMe,
    required this.createdAt,
    this.amount,
  });

  factory NegotiationEventItem.fromJson(Map<String, dynamic> j) =>
      NegotiationEventItem(
        id: j['id'] as String,
        type: (j['type'] ?? '') as String,
        amount: j['amount'] as num?,
        byMe: j['byMe'] as bool? ?? false,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}
