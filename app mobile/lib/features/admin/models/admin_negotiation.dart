import 'admin_common.dart';

/// Motivos por los que un administrador puede leer el contenido de una
/// negociación. Leerlo **exige motivo** y queda registrado en la misma operación.
const contentAccessReasonValues = <String>[
  'Report', 'Moderation', 'Dispute', 'FraudInvestigation', 'SupportRequested',
];

String contentAccessReasonLabel(String r) => switch (r) {
      'Report' => 'Signalement',
      'Moderation' => 'Modération',
      'Dispute' => 'Litige',
      'FraudInvestigation' => 'Enquête fraude',
      'SupportRequested' => 'Demande de support',
      _ => r,
    };

/// Hito de la cronología, sin la voz «vous / l’autre» (vista de admin).
String adminTimelineLabel(String? type, {String? amountLabel}) => switch (type) {
      'ConversationStarted' => 'Conversation démarrée',
      'OfferMade' => 'Offre${amountLabel != null ? ' : $amountLabel' : ''}',
      'CounterOffer' => 'Contre-offre${amountLabel != null ? ' : $amountLabel' : ''}',
      'OfferAccepted' => 'Offre acceptée',
      'OfferRejected' => 'Offre refusée',
      'ContractCreated' => 'Contrat créé',
      'ContractChangeRequested' => 'Modification demandée',
      'ContractValidated' => 'Contrat validé',
      'SaleVerified' => 'Vente vérifiée',
      _ => 'Événement',
    };

class AdminNegotiationRow {
  final String id;
  final String vehicleReference;
  final String vehicleTitle;
  final String buyerName;
  final String sellerName;
  final String status;
  final int offersCount;
  final int messagesCount;
  final String? contractReference;
  final String? contractStatus;
  final DateTime createdAt;
  final DateTime? lastActivityAt;

  const AdminNegotiationRow({
    required this.id,
    required this.vehicleReference,
    required this.vehicleTitle,
    required this.buyerName,
    required this.sellerName,
    required this.status,
    required this.offersCount,
    required this.messagesCount,
    required this.createdAt,
    this.contractReference,
    this.contractStatus,
    this.lastActivityAt,
  });

  factory AdminNegotiationRow.fromJson(Map<String, dynamic> j) =>
      AdminNegotiationRow(
        id: j['id'] as String,
        vehicleReference: (j['vehicleReference'] ?? '') as String,
        vehicleTitle: (j['vehicleTitle'] ?? '') as String,
        buyerName: (j['buyerName'] ?? '') as String,
        sellerName: (j['sellerName'] ?? '') as String,
        status: (j['status'] ?? 'EnCours') as String,
        offersCount: j['offersCount'] as int? ?? 0,
        messagesCount: j['messagesCount'] as int? ?? 0,
        contractReference: j['contractReference'] as String?,
        contractStatus: j['contractStatus'] as String?,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
        lastActivityAt: DateTime.tryParse((j['lastActivityAt'] ?? '') as String),
      );
}

class AdminNegotiationDetail {
  final AdminNegotiationRow negotiation;
  final List<AdminOffer> offers;
  final List<AdminTimelineEvent> timeline;
  final List<AdminAction> actions;

  const AdminNegotiationDetail({
    required this.negotiation,
    required this.offers,
    required this.timeline,
    required this.actions,
  });

  factory AdminNegotiationDetail.fromJson(Map<String, dynamic> j) =>
      AdminNegotiationDetail(
        negotiation:
            AdminNegotiationRow.fromJson(j['negotiation'] as Map<String, dynamic>),
        offers: (j['offers'] as List<dynamic>? ?? const [])
            .map((e) => AdminOffer.fromJson(e as Map<String, dynamic>))
            .toList(),
        timeline: (j['timeline'] as List<dynamic>? ?? const [])
            .map((e) => AdminTimelineEvent.fromJson(e as Map<String, dynamic>))
            .toList(),
        actions: (j['actions'] as List<dynamic>? ?? const [])
            .map((e) => AdminAction.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class AdminOffer {
  final String id;
  final num amount;
  final num listedPrice;
  final String status;
  final bool fromBuyer;
  final DateTime createdAt;

  const AdminOffer({
    required this.id,
    required this.amount,
    required this.listedPrice,
    required this.status,
    required this.fromBuyer,
    required this.createdAt,
  });

  factory AdminOffer.fromJson(Map<String, dynamic> j) => AdminOffer(
        id: j['id'] as String,
        amount: (j['amount'] as num?) ?? 0,
        listedPrice: (j['listedPrice'] as num?) ?? 0,
        status: (j['status'] ?? 'EnAttente') as String,
        fromBuyer: j['fromBuyer'] as bool? ?? false,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}

class AdminTimelineEvent {
  final String type;
  final num? amount;
  final DateTime createdAt;

  const AdminTimelineEvent(
      {required this.type, this.amount, required this.createdAt});

  factory AdminTimelineEvent.fromJson(Map<String, dynamic> j) =>
      AdminTimelineEvent(
        type: (j['type'] ?? '') as String,
        amount: j['amount'] as num?,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}

/// Mensaje devuelto **solo** tras un acceso justificado y registrado.
class AdminMessage {
  final String id;
  final String body;
  final bool fromBuyer;
  final DateTime createdAt;

  const AdminMessage({
    required this.id,
    required this.body,
    required this.fromBuyer,
    required this.createdAt,
  });

  factory AdminMessage.fromJson(Map<String, dynamic> j) => AdminMessage(
        id: j['id'] as String,
        body: (j['body'] ?? '') as String,
        fromBuyer: j['fromBuyer'] as bool? ?? false,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}
