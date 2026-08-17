/// El usuario autenticado, tal como lo devuelve la API (mismo contrato que la web).
///
/// Solo hay dos roles: `User` y `Admin`. `Particulier`/`Professionnel` es el
/// `accountType`, un campo informativo del perfil que NO cambia permisos.
class AppUser {
  final String id;
  final String displayName;
  final String? phone;
  final String? email;
  final String role; // 'User' | 'Admin'
  final String? accountType; // 'Particulier' | 'Professionnel'
  final String? avatarUrl;

  const AppUser({
    required this.id,
    required this.displayName,
    required this.role,
    this.phone,
    this.email,
    this.accountType,
    this.avatarUrl,
  });

  bool get isAdmin => role == 'Admin';

  factory AppUser.fromJson(Map<String, dynamic> j) => AppUser(
        id: j['id'] as String,
        displayName: (j['displayName'] ?? '') as String,
        phone: j['phone'] as String?,
        email: j['email'] as String?,
        role: (j['role'] ?? 'User') as String,
        accountType: j['accountType'] as String?,
        avatarUrl: j['avatarUrl'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'displayName': displayName,
        'phone': phone,
        'email': email,
        'role': role,
        'accountType': accountType,
        'avatarUrl': avatarUrl,
      };
}
