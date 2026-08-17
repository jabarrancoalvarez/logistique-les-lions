class Reminder {
  final String id;
  final String garageVehicleId;
  final String type;
  final String label;
  final DateTime? dueDate;
  final int? dueMileage;
  final String status;
  final int? daysRemaining;
  final int? mileageRemaining;
  final String? notes;

  const Reminder({
    required this.id,
    required this.garageVehicleId,
    required this.type,
    required this.label,
    required this.status,
    this.dueDate,
    this.dueMileage,
    this.daysRemaining,
    this.mileageRemaining,
    this.notes,
  });

  factory Reminder.fromJson(Map<String, dynamic> j) => Reminder(
        id: j['id'] as String,
        garageVehicleId: (j['garageVehicleId'] ?? '') as String,
        type: (j['type'] ?? 'Autre') as String,
        label: (j['label'] ?? '') as String,
        dueDate: DateTime.tryParse((j['dueDate'] ?? '') as String),
        dueMileage: j['dueMileage'] as int?,
        status: (j['status'] ?? 'AVenir') as String,
        daysRemaining: j['daysRemaining'] as int?,
        mileageRemaining: j['mileageRemaining'] as int?,
        notes: j['notes'] as String?,
      );
}

/// Rappel del resumen general de Mon Garage (`UpcomingReminderDto`).
class UpcomingReminder {
  final String id;
  final String garageVehicleId;
  final String vehicleTitle;
  final String type;
  final String label;
  final DateTime? dueDate;
  final int? dueMileage;
  final String status;
  final int? daysRemaining;
  final int? mileageRemaining;

  const UpcomingReminder({
    required this.id,
    required this.garageVehicleId,
    required this.vehicleTitle,
    required this.type,
    required this.label,
    required this.status,
    this.dueDate,
    this.dueMileage,
    this.daysRemaining,
    this.mileageRemaining,
  });

  factory UpcomingReminder.fromJson(Map<String, dynamic> j) => UpcomingReminder(
        id: j['id'] as String,
        garageVehicleId: (j['garageVehicleId'] ?? '') as String,
        vehicleTitle: (j['vehicleTitle'] ?? '') as String,
        type: (j['type'] ?? 'Autre') as String,
        label: (j['label'] ?? '') as String,
        dueDate: DateTime.tryParse((j['dueDate'] ?? '') as String),
        dueMileage: j['dueMileage'] as int?,
        status: (j['status'] ?? 'AVenir') as String,
        daysRemaining: j['daysRemaining'] as int?,
        mileageRemaining: j['mileageRemaining'] as int?,
      );
}
