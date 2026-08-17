// Pestaña «Contrat» de una negociación (`ContractTabDto`).

String contractStatusLabel(String? s) => switch (s) {
      'Brouillon' => 'Brouillon',
      'AValider' => 'À valider',
      'ModificationDemandee' => 'Modification demandée',
      'Valide' => 'Validé',
      'Annule' => 'Annulé',
      _ => '—',
    };

class ContractTab {
  final Contract? contract;
  final ContractPrefill prefill;
  final bool canCreate;
  final bool isSeller;

  const ContractTab({
    required this.prefill,
    required this.canCreate,
    required this.isSeller,
    this.contract,
  });

  factory ContractTab.fromJson(Map<String, dynamic> j) => ContractTab(
        contract: j['contract'] == null
            ? null
            : Contract.fromJson(j['contract'] as Map<String, dynamic>),
        prefill:
            ContractPrefill.fromJson(j['prefill'] as Map<String, dynamic>),
        canCreate: j['canCreate'] as bool? ?? false,
        isSeller: j['isSeller'] as bool? ?? false,
      );
}

class ContractPrefill {
  final String vehicleMake;
  final String? vehicleModel;
  final String? vehicleVersion;
  final int vehicleYear;
  final int? vehicleMileage;
  final String? vehicleVin;
  final String vehicleReference;
  final num suggestedPrice;
  final String sellerLegalName;
  final String buyerLegalName;

  const ContractPrefill({
    required this.vehicleMake,
    required this.vehicleYear,
    required this.vehicleReference,
    required this.suggestedPrice,
    required this.sellerLegalName,
    required this.buyerLegalName,
    this.vehicleModel,
    this.vehicleVersion,
    this.vehicleMileage,
    this.vehicleVin,
  });

  factory ContractPrefill.fromJson(Map<String, dynamic> j) => ContractPrefill(
        vehicleMake: (j['vehicleMake'] ?? '') as String,
        vehicleModel: j['vehicleModel'] as String?,
        vehicleVersion: j['vehicleVersion'] as String?,
        vehicleYear: j['vehicleYear'] as int? ?? 0,
        vehicleMileage: j['vehicleMileage'] as int?,
        vehicleVin: j['vehicleVin'] as String?,
        vehicleReference: (j['vehicleReference'] ?? '') as String,
        suggestedPrice: (j['suggestedPrice'] as num?) ?? 0,
        sellerLegalName: (j['sellerLegalName'] ?? '') as String,
        buyerLegalName: (j['buyerLegalName'] ?? '') as String,
      );
}

class Contract {
  final String id;
  final String publicReference;
  final String status;
  final String negotiationId;

  final String vehicleMake;
  final String? vehicleModel;
  final String? vehicleVersion;
  final int vehicleYear;
  final int? vehicleMileage;
  final String? vehicleVin;
  final String? registrationPlate;
  final String vehicleReference;

  final num agreedPrice;
  final DateTime saleDate;

  final String sellerLegalName;
  final String? sellerIdDocument;
  final String? sellerAddress;
  final String buyerLegalName;
  final String? buyerIdDocument;
  final String? buyerAddress;

  final bool createdByMe;
  final bool canEdit;
  final bool canSend;
  final bool canValidate;
  final bool canRequestChanges;
  final bool canCancel;
  final bool canDownloadPdf;

  final String? verificationCode;
  final String? changeRequestNotes;
  final DateTime createdAt;
  final DateTime? sentAt;
  final DateTime? validatedAt;

  const Contract({
    required this.id,
    required this.publicReference,
    required this.status,
    required this.negotiationId,
    required this.vehicleMake,
    required this.vehicleYear,
    required this.vehicleReference,
    required this.agreedPrice,
    required this.saleDate,
    required this.sellerLegalName,
    required this.buyerLegalName,
    required this.createdByMe,
    required this.canEdit,
    required this.canSend,
    required this.canValidate,
    required this.canRequestChanges,
    required this.canCancel,
    required this.canDownloadPdf,
    required this.createdAt,
    this.vehicleModel,
    this.vehicleVersion,
    this.vehicleMileage,
    this.vehicleVin,
    this.registrationPlate,
    this.sellerIdDocument,
    this.sellerAddress,
    this.buyerIdDocument,
    this.buyerAddress,
    this.verificationCode,
    this.changeRequestNotes,
    this.sentAt,
    this.validatedAt,
  });

  factory Contract.fromJson(Map<String, dynamic> j) => Contract(
        id: j['id'] as String,
        publicReference: (j['publicReference'] ?? '') as String,
        status: (j['status'] ?? 'Brouillon') as String,
        negotiationId: (j['negotiationId'] ?? '') as String,
        vehicleMake: (j['vehicleMake'] ?? '') as String,
        vehicleModel: j['vehicleModel'] as String?,
        vehicleVersion: j['vehicleVersion'] as String?,
        vehicleYear: j['vehicleYear'] as int? ?? 0,
        vehicleMileage: j['vehicleMileage'] as int?,
        vehicleVin: j['vehicleVin'] as String?,
        registrationPlate: j['registrationPlate'] as String?,
        vehicleReference: (j['vehicleReference'] ?? '') as String,
        agreedPrice: (j['agreedPrice'] as num?) ?? 0,
        saleDate:
            DateTime.tryParse((j['saleDate'] ?? '') as String) ?? DateTime.now(),
        sellerLegalName: (j['sellerLegalName'] ?? '') as String,
        sellerIdDocument: j['sellerIdDocument'] as String?,
        sellerAddress: j['sellerAddress'] as String?,
        buyerLegalName: (j['buyerLegalName'] ?? '') as String,
        buyerIdDocument: j['buyerIdDocument'] as String?,
        buyerAddress: j['buyerAddress'] as String?,
        createdByMe: j['createdByMe'] as bool? ?? false,
        canEdit: j['canEdit'] as bool? ?? false,
        canSend: j['canSend'] as bool? ?? false,
        canValidate: j['canValidate'] as bool? ?? false,
        canRequestChanges: j['canRequestChanges'] as bool? ?? false,
        canCancel: j['canCancel'] as bool? ?? false,
        canDownloadPdf: j['canDownloadPdf'] as bool? ?? false,
        verificationCode: j['verificationCode'] as String?,
        changeRequestNotes: j['changeRequestNotes'] as String?,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
        sentAt: DateTime.tryParse((j['sentAt'] ?? '') as String),
        validatedAt: DateTime.tryParse((j['validatedAt'] ?? '') as String),
      );
}
