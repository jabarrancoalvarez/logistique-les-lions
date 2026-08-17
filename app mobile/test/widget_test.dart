import 'package:flutter_test/flutter_test.dart';
import 'package:yoon_u_auto/core/data/senegal_regions.dart';
import 'package:yoon_u_auto/core/theme/app_theme.dart';
import 'package:yoon_u_auto/core/util/fcfa.dart';
import 'package:yoon_u_auto/features/auth/models/app_user.dart';
import 'package:yoon_u_auto/features/vehicles/models/vehicle_filters.dart';
import 'package:yoon_u_auto/features/vehicles/models/vehicle_summary.dart';

void main() {
  test('fcfa formatea con separador de millares y sufijo', () {
    expect(fcfa(8900000), '8.900.000 FCFA');
    expect(fcfa(8900000, withSuffix: false), '8.900.000');
    expect(fcfa(null), '');
    expect(fcfa(500), '500 FCFA');
  });

  test('VehicleSummary se parsea desde la tarjeta de la API', () {
    final v = VehicleSummary.fromJson({
      'id': 'v1',
      'publicReference': 'YU12345',
      'slug': 'toyota-corolla-yu12345',
      'title': 'Toyota Corolla',
      'makeName': 'Toyota',
      'year': 2020,
      'price': 8900000,
      'condition': 'Used',
      'status': 'Actif',
      'sellerId': 's1',
      'images': ['a.jpg', 'b.jpg'],
      'imageCount': 2,
      'isFeatured': false,
      'favoritesCount': 0,
      'viewsCount': 3,
      'fuelType': 'Diesel',
      'priceIndicator': 'BonneAffaire',
    });
    expect(v.title, 'Toyota Corolla');
    expect(v.coverImage, 'a.jpg');
    expect(v.priceIndicator, 'BonneAffaire');
  });

  test('VehicleFilters cuenta filtros activos y arma la query', () {
    const f = VehicleFilters(
      search: 'toyota',
      makeId: 'm1',
      priceTo: 10000000,
      fuelType: 'Diesel',
    );
    // search no cuenta; makeId + rango de precio + fuel = 3
    expect(f.activeCount, 3);
    final q = f.toQueryParameters();
    expect(q['search'], 'toyota');
    expect(q['fuelType'], 'Diesel');
    expect(q.containsKey('region'), isFalse);
  });

  test('Senegal tiene las 14 regiones con códigos únicos', () {
    expect(senegalRegions.length, 14);
    final codes = senegalRegions.map((r) => r.code).toSet();
    expect(codes.length, 14);
    expect(codes, contains('DK'));
  });

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
