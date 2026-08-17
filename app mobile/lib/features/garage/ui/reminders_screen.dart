import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../models/garage_enums.dart';
import '../models/reminder.dart';
import '../providers/garage_providers.dart';

/// Rappels — recordatorios de un vehículo por fecha, kilometraje o ambos.
class RemindersScreen extends ConsumerWidget {
  const RemindersScreen({super.key, required this.vehicleId});
  final String vehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(remindersProvider(vehicleId));

    return Scaffold(
      appBar: AppBar(title: const Text('Rappels')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _openForm(context, ref),
        icon: const Icon(Icons.add),
        label: const Text('Rappel'),
      ),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(remindersProvider(vehicleId)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (list) {
          if (list.isEmpty) return const _Empty();
          return ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
            itemCount: list.length,
            separatorBuilder: (_, _) => const SizedBox(height: 10),
            itemBuilder: (_, i) => _ReminderTile(
              reminder: list[i],
              onEdit: () => _openForm(context, ref, reminder: list[i]),
              onStatus: (s) => _setStatus(context, ref, list[i], s),
              onDelete: () => _delete(context, ref, list[i]),
            ),
          );
        },
      ),
    );
  }

  Future<void> _openForm(BuildContext context, WidgetRef ref,
      {Reminder? reminder}) async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _ReminderForm(vehicleId: vehicleId, reminder: reminder),
    );
    if (saved == true) ref.invalidate(remindersProvider(vehicleId));
  }

  Future<void> _setStatus(
      BuildContext context, WidgetRef ref, Reminder r, String status) async {
    try {
      await ref.read(garageRepositoryProvider).setReminderStatus(r.id, status);
      ref.invalidate(remindersProvider(vehicleId));
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Action impossible.')),
        );
      }
    }
  }

  Future<void> _delete(BuildContext context, WidgetRef ref, Reminder r) async {
    try {
      await ref.read(garageRepositoryProvider).deleteReminder(r.id);
      ref.invalidate(remindersProvider(vehicleId));
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Suppression impossible.')),
        );
      }
    }
  }
}

class _ReminderTile extends StatelessWidget {
  const _ReminderTile({
    required this.reminder,
    required this.onEdit,
    required this.onStatus,
    required this.onDelete,
  });
  final Reminder reminder;
  final VoidCallback onEdit;
  final ValueChanged<String> onStatus;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final r = reminder;
    final open = reminderIsOpen(r.status);
    final overdue = (r.daysRemaining != null && r.daysRemaining! < 0) ||
        (r.mileageRemaining != null && r.mileageRemaining! < 0);

    return Card(
      margin: EdgeInsets.zero,
      child: ListTile(
        onTap: onEdit,
        leading: Icon(
          open ? Icons.notifications_active_outlined : Icons.check_circle,
          color: open
              ? (overdue ? AppColors.error : AppColors.azureDark)
              : AppColors.silver,
        ),
        title: Text(r.label,
            style: TextStyle(
                fontWeight: FontWeight.w700,
                decoration: r.status == 'Annule'
                    ? TextDecoration.lineThrough
                    : null)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 2),
            Text([
              reminderTypeLabel(r.type),
              if (r.dueDate != null)
                DateFormat('d MMM yyyy', 'fr').format(r.dueDate!.toLocal()),
              if (r.dueMileage != null)
                '${fcfa(r.dueMileage, withSuffix: false)} km',
            ].join(' · ')),
            Text(
              _remaining(r),
              style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: overdue ? AppColors.error : AppColors.steel),
            ),
          ],
        ),
        trailing: PopupMenuButton<String>(
          onSelected: (c) {
            if (c == 'delete') {
              onDelete();
            } else {
              onStatus(c);
            }
          },
          itemBuilder: (_) => [
            if (open)
              const PopupMenuItem(value: 'Termine', child: Text('Marquer terminé')),
            if (open)
              const PopupMenuItem(value: 'Annule', child: Text('Annuler')),
            if (!open)
              const PopupMenuItem(value: 'AVenir', child: Text('Rouvrir')),
            const PopupMenuItem(value: 'delete', child: Text('Supprimer')),
          ],
        ),
      ),
    );
  }

  String _remaining(Reminder r) {
    if (!reminderIsOpen(r.status)) return reminderStatusLabel(r.status);
    if (r.daysRemaining != null) {
      final d = r.daysRemaining!;
      if (d < 0) return 'En retard de ${-d} j';
      if (d == 0) return "Aujourd'hui";
      return 'Dans $d j';
    }
    if (r.mileageRemaining != null) {
      final m = r.mileageRemaining!;
      if (m < 0) return 'Dépassé de ${fcfa(-m, withSuffix: false)} km';
      return 'Dans ${fcfa(m, withSuffix: false)} km';
    }
    return reminderStatusLabel(r.status);
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
            Icon(Icons.notifications_none, size: 56, color: AppColors.silver),
            SizedBox(height: 16),
            Text('Aucun rappel',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            SizedBox(height: 6),
            Text('Créez un rappel par date, par kilométrage ou les deux.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.steel)),
          ],
        ),
      ),
    );
  }
}

class _ReminderForm extends ConsumerStatefulWidget {
  const _ReminderForm({required this.vehicleId, this.reminder});
  final String vehicleId;
  final Reminder? reminder;

  @override
  ConsumerState<_ReminderForm> createState() => _ReminderFormState();
}

class _ReminderFormState extends ConsumerState<_ReminderForm> {
  late String _type = widget.reminder?.type ?? 'Vidange';
  late DateTime? _dueDate = widget.reminder?.dueDate;
  late final _label =
      TextEditingController(text: widget.reminder?.label ?? '');
  late final _mileage = TextEditingController(
      text: widget.reminder?.dueMileage?.toString() ?? '');
  bool _saving = false;

  @override
  void dispose() {
    _label.dispose();
    _mileage.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_label.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Ajoutez un libellé.')),
      );
      return;
    }
    final mileage =
        _mileage.text.trim().isEmpty ? null : int.tryParse(_mileage.text.trim());
    if (_dueDate == null && mileage == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Indiquez une date ou un kilométrage.')),
      );
      return;
    }
    setState(() => _saving = true);
    final body = {
      'type': _type,
      'label': _label.text.trim(),
      'dueDate': _dueDate?.toUtc().toIso8601String(),
      'dueMileage': mileage,
      'notes': null,
    };
    try {
      final repo = ref.read(garageRepositoryProvider);
      if (widget.reminder == null) {
        await repo.addReminder(widget.vehicleId, body);
      } else {
        await repo.updateReminder(widget.reminder!.id, body);
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
      initialDate: _dueDate ?? now,
      firstDate: DateTime(now.year - 1),
      lastDate: DateTime(now.year + 10),
    );
    if (picked != null) setState(() => _dueDate = picked);
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
              Text(widget.reminder == null ? 'Nouveau rappel' : 'Modifier',
                  style: const TextStyle(
                      fontSize: 18, fontWeight: FontWeight.w800)),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: _type,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Type'),
                items: [
                  for (final t in reminderTypeValues)
                    DropdownMenuItem(
                        value: t, child: Text(reminderTypeLabel(t))),
                ],
                onChanged: (v) => setState(() => _type = v ?? 'Vidange'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _label,
                decoration: const InputDecoration(
                    labelText: 'Libellé', hintText: 'Ex. Vidange moteur'),
              ),
              const SizedBox(height: 12),
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.event_outlined),
                title: const Text('Échéance (date)'),
                subtitle: Text(_dueDate == null
                    ? 'Aucune'
                    : DateFormat('d MMMM yyyy', 'fr').format(_dueDate!)),
                trailing: _dueDate == null
                    ? const Icon(Icons.add, size: 18)
                    : IconButton(
                        icon: const Icon(Icons.clear, size: 18),
                        onPressed: () => setState(() => _dueDate = null),
                      ),
                onTap: _pickDate,
              ),
              TextField(
                controller: _mileage,
                keyboardType: TextInputType.number,
                inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                decoration:
                    const InputDecoration(labelText: 'Échéance (kilométrage)'),
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
