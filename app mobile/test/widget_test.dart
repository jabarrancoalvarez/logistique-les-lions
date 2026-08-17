import 'package:flutter_test/flutter_test.dart';
import 'package:yoon_u_auto/core/theme/app_theme.dart';
import 'package:yoon_u_auto/features/auth/models/app_user.dart';

void main() {
  test('AppUser se parsea desde el JSON de la API', () {
    final user = AppUser.fromJson({
      'id': 'abc',
      'displayName': 'QA Particulier',
      'phone': '+221771234501',
      'role': 'User',
      'accountType': 'Particulier',
    });
    expect(user.displayName, 'QA Particulier');
    expect(user.isAdmin, isFalse);
    expect(user.accountType, 'Particulier');
  });

  test('AppUser reconoce el rol de administrador', () {
    final admin = AppUser.fromJson({'id': '1', 'displayName': 'QA', 'role': 'Admin'});
    expect(admin.isAdmin, isTrue);
  });

  test('El tema de marca se construye', () {
    final theme = AppTheme.light();
    expect(theme.useMaterial3, isTrue);
  });
}
