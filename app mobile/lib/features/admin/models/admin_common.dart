/// Fila de `admin_actions` (append-only): quién hizo qué y con qué motivo.
class AdminAction {
  final String id;
  final String type;
  final String? reason;
  final String adminName;
  final DateTime createdAt;

  const AdminAction({
    required this.id,
    required this.type,
    required this.adminName,
    required this.createdAt,
    this.reason,
  });

  factory AdminAction.fromJson(Map<String, dynamic> j) => AdminAction(
        id: j['id'] as String,
        type: (j['type'] ?? '') as String,
        reason: j['reason'] as String?,
        adminName: (j['adminName'] ?? '') as String,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}

class AdminNote {
  final String id;
  final String body;
  final String adminName;
  final DateTime createdAt;

  const AdminNote({
    required this.id,
    required this.body,
    required this.adminName,
    required this.createdAt,
  });

  factory AdminNote.fromJson(Map<String, dynamic> j) => AdminNote(
        id: j['id'] as String,
        body: (j['body'] ?? '') as String,
        adminName: (j['adminName'] ?? '') as String,
        createdAt:
            DateTime.tryParse((j['createdAt'] ?? '') as String) ?? DateTime.now(),
      );
}

/// Página genérica del backoffice (`{ totalCount, page, pageSize, items }`).
class AdminPage<T> {
  final int totalCount;
  final int page;
  final int pageSize;
  final List<T> items;

  const AdminPage({
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.items,
  });

  factory AdminPage.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) itemFromJson,
  ) =>
      AdminPage(
        totalCount: json['totalCount'] as int? ?? 0,
        page: json['page'] as int? ?? 1,
        pageSize: json['pageSize'] as int? ?? 0,
        items: (json['items'] as List<dynamic>? ?? const [])
            .map((e) => itemFromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
