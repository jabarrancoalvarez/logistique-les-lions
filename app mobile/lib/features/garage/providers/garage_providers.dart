import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../data/garage_repository.dart';
import '../models/garage_document.dart';
import '../models/garage_models.dart';
import '../models/maintenance.dart';
import '../models/reminder.dart';
import '../models/transparency.dart';
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

/// Documentos privados de un vehículo.
final documentsProvider =
    FutureProvider.autoDispose.family<List<GarageDocument>, String>(
  (ref, vehicleId) =>
      ref.watch(garageRepositoryProvider).getDocuments(vehicleId),
);

/// Ajustes de «Transparence» de un anuncio creado desde el garaje.
final transparencyProvider =
    FutureProvider.autoDispose.family<TransparencySettings, String>(
  (ref, listedVehicleId) =>
      ref.watch(garageRepositoryProvider).getTransparency(listedVehicleId),
);

/// Bytes de una foto privada del garaje. Cacheado por id de imagen.
final garageImageBytesProvider =
    FutureProvider.family<List<int>, String>(
  (ref, imageId) => ref.watch(garageRepositoryProvider).imageBytes(imageId),
);
