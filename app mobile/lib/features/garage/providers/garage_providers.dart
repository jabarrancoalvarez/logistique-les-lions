import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../data/garage_repository.dart';
import '../models/garage_models.dart';
import '../models/maintenance.dart';
import '../models/reminder.dart';
import '../models/valuation.dart';

final garageRepositoryProvider = Provider<GarageRepository>(
  (ref) => GarageRepository(ref.watch(apiClientProvider)),
);

/// Resumen de Mon Garage (tarjetas + totales).
final garageSummaryProvider = FutureProvider.autoDispose<GarageSummary>(
  (ref) => ref.watch(garageRepositoryProvider).getGarage(),
);

final garageVehicleProvider =
    FutureProvider.autoDispose.family<GarageVehicleDetail, String>(
  (ref, id) => ref.watch(garageRepositoryProvider).getVehicle(id),
);

final maintenanceProvider =
    FutureProvider.autoDispose.family<MaintenanceHistory, String>(
  (ref, vehicleId) =>
      ref.watch(garageRepositoryProvider).getMaintenance(vehicleId),
);

final remindersProvider =
    FutureProvider.autoDispose.family<List<Reminder>, String>(
  (ref, vehicleId) =>
      ref.watch(garageRepositoryProvider).getReminders(vehicleId),
);

final valuationProvider =
    FutureProvider.autoDispose.family<Valuation, String>(
  (ref, vehicleId) =>
      ref.watch(garageRepositoryProvider).getValuation(vehicleId),
);

final completenessProvider =
    FutureProvider.autoDispose.family<Completeness, String>(
  (ref, vehicleId) =>
      ref.watch(garageRepositoryProvider).getCompleteness(vehicleId),
);
