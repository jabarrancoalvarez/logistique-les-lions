import 'admin_common.dart';

class ReportRow {
  final String id;
  final String publicReference;
  final String targetType;
  final String targetId;
  final String targetLabel;
  final String reporterId;
  final String reporterName;
  final String? reportedUserId;
  final String? reportedUserName;
  final String reason;
  final String? description;
  final String status;
  final DateTime createdAt;

  const ReportRow({
    required this.id,
    required this.publicReference,
    required this.targetType,
    required this.targetId,
    required this.targetLabel,
    required this.reporterId,
    required this.reporterName,
    required this.reason,
    required this.status,
    required this.createdAt,
    this.reportedUserId,
    this.reportedUserName,
    this.description,
  });

  factory ReportRow.fromJson(Map<String, dynamic> j) => ReportRow(
        id: j['id'] as String,
        publicReference: (j['publicReference'] ?? '') as String,
        targetType: (j['targetType'] ?? 'Listing') as String,
        targetId: (j['targetId'] ?? '') as String,
        targetLabel: (j['targetLabel'] ?? '') as String,
        reporterId: (j['reporterId'] ?? '') as String,
        reporterName: (j['reporterName'] ?? '') as String,
        reportedUserId: j['reportedUserId'] as String?,
        reportedUserName: j['reportedUserName'] as String?,
        reason: (j['reason'] ?? 'Autre') as String,
        description: j['description'] as String?,
        status: (j['status'] ?? 'Nouveau') as String,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}

class ReportList {
  final int totalCount;
  final int page;
  final int pageSize;
  final Map<String, int> countByStatus;
  final List<ReportRow> items;

  const ReportList({
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.countByStatus,
    required this.items,
  });

  factory ReportList.fromJson(Map<String, dynamic> j) => ReportList(
        totalCount: j['totalCount'] as int? ?? 0,
        page: j['page'] as int? ?? 1,
        pageSize: j['pageSize'] as int? ?? 0,
        countByStatus: (j['countByStatus'] as Map<String, dynamic>? ?? const {})
            .map((k, v) => MapEntry(k, v as int)),
        items: (j['items'] as List<dynamic>? ?? const [])
            .map((e) => ReportRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ReportDetail {
  final ReportRow report;
  final List<String> evidence;
  final String? resolution;
  final DateTime? resolvedAt;
  final String? handledByAdminName;
  final int otherOpenReports;
  final List<AdminAction> actions;
  final List<AdminNote> notes;

  const ReportDetail({
    required this.report,
    required this.evidence,
    required this.otherOpenReports,
    required this.actions,
    required this.notes,
    this.resolution,
    this.resolvedAt,
    this.handledByAdminName,
  });

  factory ReportDetail.fromJson(Map<String, dynamic> j) => ReportDetail(
        report: ReportRow.fromJson(j['report'] as Map<String, dynamic>),
        evidence: (j['evidence'] as List<dynamic>? ?? const [])
            .map((e) => e as String)
            .toList(),
        resolution: j['resolution'] as String?,
        resolvedAt: DateTime.tryParse((j['resolvedAt'] ?? '') as String),
        handledByAdminName: j['handledByAdminName'] as String?,
        otherOpenReports: j['otherOpenReports'] as int? ?? 0,
        actions: (j['actions'] as List<dynamic>? ?? const [])
            .map((e) => AdminAction.fromJson(e as Map<String, dynamic>))
            .toList(),
        notes: (j['notes'] as List<dynamic>? ?? const [])
            .map((e) => AdminNote.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
