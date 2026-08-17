/// Marca de vehículo, para el selector del buscador (`VehicleMakeDto`).
class VehicleMake {
  final String id;
  final String name;
  final bool isPopular;
  final int modelsCount;

  const VehicleMake({
    required this.id,
    required this.name,
    required this.isPopular,
    required this.modelsCount,
  });

  factory VehicleMake.fromJson(Map<String, dynamic> j) => VehicleMake(
        id: j['id'] as String,
        name: (j['name'] ?? '') as String,
        isPopular: j['isPopular'] as bool? ?? false,
        modelsCount: j['modelsCount'] as int? ?? 0,
      );
}
