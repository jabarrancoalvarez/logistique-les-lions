import 'admin_common.dart';
import 'admin_negotiation.dart';

class AdminContractRow {
  final String id;
  final String publicReference;
  final String negotiationId;
  final String vehicleReference;
  final String vehicleLabel;
  final String sellerName;
  final String buyerName;
  final num agreedPrice;
  final String status;
  final DateTime saleDate;
  final DateTime createdAt;
  final DateTime? validatedAt;
  final bool isVerifiedSale;

  const AdminContractRow({
    required this.id,
    required this.publicReference,
    required this.negotiationId,
    required this.vehicleReference,
    required this.vehicleLabel,
    required this.sellerName,
    required this.buyerName,
    required this.agreedPrice,
    required this.status,
    required this.saleDate,
    required this.createdAt,
    required this.isVerifiedSale,
    this.validatedAt,
  });

  factory AdminContractRow.fromJson(Map<String, dynamic> j) => AdminContractRow(
        id: j['id'] as String,
        publicReference: (j['publicReference'] ?? '') as String,
        negotiationId: (j['negotiationId'] ?? '') as String,
        vehicleReference: (j['vehicleReference'] ?? '') as String,
        vehicleLabel: (j['vehicleLabel'] ?? '') as String,
        sellerName: (j['sellerName'] ?? '') as String,
        buyerName: (j['buyerName'] ?? '') as String,
        agreedPrice: (j['agreedPrice'] as num?) ?? 0,
        status: (j['status'] ?? 'Brouillon') as String,
        saleDate:
            DateTime.tryParse((j['saleDate'] ?? '') as String) ?? DateTime.now(),
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
        validatedAt: DateTime.tryParse((j['validatedAt'] ?? '') as String),
        isVerifiedSale: j['isVerifiedSale'] as bool? ?? false,
      );
}

class AdminContractDetail {
  final AdminContractRow contract;
  final String? vehicleModel;
  final String? vehicleVersion;
  final int vehicleYear;
  final int? vehicleMileage;
  final String? vehicleVin;
  final String? registrationPlate;
  final String? verificationCode;
  final String? changeRequestNotes;
  final List<AdminTimelineEvent> timeline;
  final List<AdminAction> actions;
  final List<AdminNote> notes;

  const AdminContractDetail({
    required this.contract,
    required this.vehicleYear,
    required this.timeline,
    required this.actions,
    required this.notes,
    this.vehicleModel,
    this.vehicleVersion,
    this.vehicleMileage,
    this.vehicleVin,
    this.registrationPlate,
    this.verificationCode,
    this.changeRequestNotes,
  });

  factory AdminContractDetail.fromJson(Map<String, dynamic> j) =>
      AdminContractDetail(
        contract: AdminContractRow.fromJson(j['contract'] as Map<String, dynamic>),
        vehicleModel: j['vehicleModel'] as String?,
        vehicleVersion: j['vehicleVersion'] as String?,
        vehicleYear: j['vehicleYear'] as int? ?? 0,
        vehicleMileage: j['vehicleMileage'] as int?,
        vehicleVin: j['vehicleVin'] as String?,
        registrationPlate: j['registrationPlate'] as String?,
        verificationCode: j['verificationCode'] as String?,
        changeRequestNotes: j['changeRequestNotes'] as String?,
        timeline: (j['timeline'] as List<dynamic>? ?? const [])
            .map((e) => AdminTimelineEvent.fromJson(e as Map<String, dynamic>))
            .toList(),
        actions: (j['actions'] as List<dynamic>? ?? const [])
            .map((e) => AdminAction.fromJson(e as Map<String, dynamic>))
            .toList(),
        notes: (j['notes'] as List<dynamic>? ?? const [])
            .map((e) => AdminNote.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
