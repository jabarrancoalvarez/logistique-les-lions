/// Fila de «Mes négociations» (`NegotiationSummaryDto`).
class NegotiationSummary {
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

  final String otherUserId;
  final String otherUserName;
  final String? otherUserAvatarUrl;

  final String? lastMessage;
  final DateTime? lastActivityAt;
  final int unreadCount;

  const NegotiationSummary({
    required this.id,
    required this.status,
    required this.isBuyer,
    required this.vehicleId,
    required this.vehicleSlug,
    required this.vehicleTitle,
    required this.vehiclePublicReference,
    required this.vehiclePrice,
    required this.vehicleStatus,
    required this.otherUserId,
    required this.otherUserName,
    required this.unreadCount,
    this.vehicleThumbnailUrl,
    this.otherUserAvatarUrl,
    this.lastMessage,
    this.lastActivityAt,
  });

  factory NegotiationSummary.fromJson(Map<String, dynamic> j) =>
      NegotiationSummary(
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
        otherUserId: (j['otherUserId'] ?? '') as String,
        otherUserName: (j['otherUserName'] ?? '') as String,
        otherUserAvatarUrl: j['otherUserAvatarUrl'] as String?,
        lastMessage: j['lastMessage'] as String?,
        lastActivityAt: DateTime.tryParse((j['lastActivityAt'] ?? '') as String),
        unreadCount: j['unreadCount'] as int? ?? 0,
      );
}
