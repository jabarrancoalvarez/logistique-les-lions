import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../models/garage_enums.dart';
import '../models/maintenance.dart';
import '../providers/garage_providers.dart';

/// Entretien — historial de mantenimiento de un vehículo, agrupado por año.
class MaintenanceScreen extends ConsumerWidget {
  const MaintenanceScreen({super.key, required this.vehicleId});
  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(maintenanceProvider(vehicleId));

    return Scaffold(
      appBar: AppBar(title: const Text('Entretien')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _openForm(context, ref),
        icon: const Icon(Icons.add),
        label: const Text('Intervention'),
      ),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(maintenanceProvider(vehicleId)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (history) {
          if (history.years.isEmpty) return const _Empty();
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
            children: [
              _Header(history: history),
              const SizedBox(height: 16),
              for (final year in history.years) ...[
                Padding(
                  padding: const EdgeInsets.only(bottom: 8, top: 8),
                  child: Text('${year.year}',
                      style: const TextStyle(
                          fontWeight: FontWeight.w800,
                          fontSize: 16,
                          color: AppColors.navy)),
                ),
                for (final r in year.records)
                  _RecordTile(
                    record: r,
                    onEdit: () => _openForm(context, ref, record: r),
                    onDelete: () => _delete(context, ref, r),
                  ),
              ],
            ],
          );
        },
      ),
    );
  }

  Future<void> _delete(
      BuildContext context, WidgetRef ref, MaintenanceRecord r) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Supprimer l’intervention ?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Annuler')),
          FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.error),
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Supprimer')),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(garageRepositoryProvider).deleteMaintenance(r.id);
      ref.invalidate(maintenanceProvider(vehicleId));
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Suppression impossible.')),
        );
      }
    }
  }

  Future<void> _openForm(BuildContext context, WidgetRef ref,
      {MaintenanceRecord? record}) async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _MaintenanceForm(vehicleId: vehicleId, record: record),
    );
    if (saved == true) ref.invalidate(maintenanceProvider(vehicleId));
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.history});
  final MaintenanceHistory history;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.frost,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.frostDark),
      ),
      child: Row(
        children: [
          _stat('${history.recordCount}', 'interventions'),
          _stat(fcfa(history.totalCost, withSuffix: false), 'FCFA au total'),
          if (history.lastMileage != null)
            _stat(fcfa(history.lastMileage, withSuffix: false),
                'km (dernier)'),
        ],
      ),
    );
  }

  Widget _stat(String value, String label) => Expanded(
        child: Column(
          children: [
            Text(value,
                style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    color: AppColors.navy,
                    fontSize: 16)),
            Text(label,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 11, color: AppColors.steel)),
          ],
        ),
      );
}

class _RecordTile extends StatelessWidget {
  const _RecordTile(
      {required this.record, required this.onEdit, required this.onDelete});
  final MaintenanceRecord record;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        onTap: onEdit,
        title: Text(maintenanceTypeLabel(record.type),
            style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 2),
            Text([
              DateFormat('d MMM yyyy', 'fr').format(record.performedAt.toLocal()),
              if (record.mileage != null)
                '${fcfa(record.mileage, withSuffix: false)} km',
              if (record.cost != null) fcfa(record.cost),
            ].join(' · ')),
            if (record.description.isNotEmpty)
              Text(record.description,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 12)),
            if (record.hasInvoice)
              const Padding(
                padding: EdgeInsets.only(top: 2),
                child: Text('Facture disponible ✓',
                    style: TextStyle(
                        fontSize: 11,
                        color: AppColors.success,
                        fontWeight: FontWeight.w600)),
              ),
          ],
        ),
        trailing: IconButton(
          icon: const Icon(Icons.delete_outline, color: AppColors.steel),
          onPressed: onDelete,
        ),
      ),
    );
  }
}

class _Empty extends StatelessWidget {
  const _Empty();
  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.build_outlined, size: 56, color: AppColors.silver),
            SizedBox(height: 16),
            Text('Aucune intervention',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            SizedBox(height: 6),
            Text('Enregistrez vidanges, freins, pneus… pour garder l’historique.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.steel)),
          ],
        ),
      ),
    );
  }
}

class _MaintenanceForm extends ConsumerStatefulWidget {
  const _MaintenanceForm({required this.vehicleId, this.record});
  final String vehicleId;
  final MaintenanceRecord? record;

  @override
  ConsumerState<_MaintenanceForm> createState() => _MaintenanceFormState();
}

class _MaintenanceFormState extends ConsumerState<_MaintenanceForm> {
  late String _type = widget.record?.type ?? 'Vidange';
  late DateTime _date = widget.record?.performedAt ?? DateTime.now();
  late final _mileage =
      TextEditingController(text: widget.record?.mileage?.toString() ?? '');
  late final _description =
      TextEditingController(text: widget.record?.description ?? '');
  late final _cost =
      TextEditingController(text: widget.record?.cost?.round().toString() ?? '');
  late final _workshop =
      TextEditingController(text: widget.record?.workshop ?? '');
  bool _saving = false;

  @override
  void dispose() {
    _mileage.dispose();
    _description.dispose();
    _cost.dispose();
    _workshop.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_description.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Ajoutez une description.')),
      );
      return;
    }
    setState(() => _saving = true);
    final body = {
      'type': _type,
      'performedAt': _date.toUtc().toIso8601String(),
      'mileage': _mileage.text.trim().isEmpty
          ? null
          : int.tryParse(_mileage.text.trim()),
      'description': _description.text.trim(),
      'cost':
          _cost.text.trim().isEmpty ? null : int.tryParse(_cost.text.trim()),
      'workshop': _workshop.text.trim().isEmpty ? null : _workshop.text.trim(),
      'notes': null,
      'documentId': widget.record?.documentId,
    };
    try {
      final repo = ref.read(garageRepositoryProvider);
      if (widget.record == null) {
        await repo.addMaintenance(widget.vehicleId, body);
      } else {
        await repo.updateMaintenance(widget.record!.id, body);
      }
      if (mounted) Navigator.pop(context, true);
    } catch (_) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Enregistrement impossible.')),
      );
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year - 40),
      lastDate: now,
    );
    if (picked != null) setState(() => _date = picked);
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: Container(
        decoration: const BoxDecoration(
          color: AppColors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: AppColors.silver,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 14),
              Text(widget.record == null ? 'Nouvelle intervention' : 'Modifier',
                  style: const TextStyle(
                      fontSize: 18, fontWeight: FontWeight.w800)),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: _type,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Type'),
                items: [
                  for (final t in maintenanceTypeValues)
                    DropdownMenuItem(
                        value: t, child: Text(maintenanceTypeLabel(t))),
                ],
                onChanged: (v) => setState(() => _type = v ?? 'Vidange'),
              ),
              const SizedBox(height: 12),
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.event_outlined),
                title: const Text('Date'),
                subtitle:
                    Text(DateFormat('d MMMM yyyy', 'fr').format(_date)),
                trailing: const Icon(Icons.edit_outlined, size: 18),
                onTap: _pickDate,
              ),
              TextField(
                controller: _mileage,
                keyboardType: TextInputType.number,
                inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                decoration: const InputDecoration(labelText: 'Kilométrage'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _description,
                maxLines: 2,
                decoration: const InputDecoration(labelText: 'Description'),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _cost,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(labelText: 'Coût (FCFA)'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextField(
                      controller: _workshop,
                      decoration: const InputDecoration(labelText: 'Atelier'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 20),
              FilledButton(
                onPressed: _saving ? null : _save,
                style: FilledButton.styleFrom(
                    minimumSize: const Size.fromHeight(48)),
                child: _saving
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white))
                    : const Text('Enregistrer'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
