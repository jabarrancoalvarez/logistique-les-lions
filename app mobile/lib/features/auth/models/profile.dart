/// Perfil completo del usuario autenticado (`ProfileDto`, de `GET /auth/me`).
/// El teléfono es la identidad verificada: se muestra pero no se edita.
class Profile {
  final String id;
  final String displayName;
  final String? phone;
  final bool phoneVerified;
  final String? email;
  final String role;
  final String accountType; // Particulier | Professionnel
  final String? avatarUrl;
  final String? region;
  final String? city;
  final String? bio;
  final bool allowWhatsAppContact;
  final int verifiedSalesCount;
  final int activeListingsCount;

  const Profile({
    required this.id,
    required this.displayName,
    required this.role,
    required this.accountType,
    required this.phoneVerified,
    required this.allowWhatsAppContact,
    required this.verifiedSalesCount,
    required this.activeListingsCount,
    this.phone,
    this.email,
    this.avatarUrl,
    this.region,
    this.city,
    this.bio,
  });

  factory Profile.fromJson(Map<String, dynamic> j) => Profile(
        id: j['id'] as String,
        displayName: (j['displayName'] ?? '') as String,
        phone: j['phone'] as String?,
        phoneVerified: j['phoneVerified'] as bool? ?? false,
        email: j['email'] as String?,
        role: (j['role'] ?? 'User') as String,
        accountType: (j['accountType'] ?? 'Particulier') as String,
        avatarUrl: j['avatarUrl'] as String?,
        region: j['region'] as String?,
        city: j['city'] as String?,
        bio: j['bio'] as String?,
        allowWhatsAppContact: j['allowWhatsAppContact'] as bool? ?? false,
        verifiedSalesCount: j['verifiedSalesCount'] as int? ?? 0,
        activeListingsCount: j['activeListingsCount'] as int? ?? 0,
      );
}
