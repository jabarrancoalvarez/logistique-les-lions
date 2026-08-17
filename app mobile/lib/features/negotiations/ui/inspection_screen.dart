import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../models/inspection.dart';
import '../providers/negotiation_providers.dart';

/// Checklist privada de inspección: solo la ve su autor, nunca la otra parte.
class InspectionScreen extends ConsumerWidget {
  const InspectionScreen({super.key, required this.negotiationId});
  final String negotiationId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(inspectionProvider(negotiationId));

    return Scaffold(
      appBar: AppBar(title: const Text('Inspection')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(inspectionProvider(negotiationId)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (inspection) =>
            _Form(negotiationId: negotiationId, inspection: inspection),
      ),
    );
  }
}

class _Form extends ConsumerStatefulWidget {
  const _Form({required this.negotiationId, required this.inspection});
  final String negotiationId;
  final Inspection inspection;

  @override
  ConsumerState<_Form> createState() => _FormState();
}

class _FormState extends ConsumerState<_Form> {
  late DateTime? _visitedAt = widget.inspection.visitedAt;
  late final _mileage = TextEditingController(
      text: widget.inspection.observedMileage?.toString() ?? '');
  late final _notes =
      TextEditingController(text: widget.inspection.notes ?? '');
  late final Map<String, String?> _results = {
    for (final it in widget.inspection.items) it.type: it.result,
  };
  late final Map<String, String?> _itemNotes = {
    for (final it in widget.inspection.items) it.type: it.notes,
  };
  bool _saving = false;

  @override
  void dispose() {
    _mileage.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    final items = [
      for (final type in inspectionItemTypes)
        InspectionItem(
            type: type, result: _results[type], notes: _itemNotes[type]),
    ];
    try {
      await ref.read(negotiationRepositoryProvider).saveInspection(
            widget.negotiationId,
            visitedAt: _visitedAt,
            observedMileage: int.tryParse(_mileage.text.trim()),
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
            items: items,
          );
      ref.invalidate(inspectionProvider(widget.negotiationId));
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Inspection enregistrée.')),
      );
      Navigator.of(context).pop();
    } catch (_) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Enregistrement impossible. Réessayez.')),
      );
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _visitedAt ?? now,
      firstDate: DateTime(now.year - 1),
      lastDate: now,
    );
    if (picked != null) setState(() => _visitedAt = picked);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.frost,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppColors.frostDark),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.lock_outline,
                        size: 18, color: AppColors.steel),
                    const SizedBox(width: 8),
                    const Expanded(
                      child: Text(
                        'Cette checklist est privée : l’autre partie ne la voit jamais.',
                        style: TextStyle(fontSize: 12, color: AppColors.steel),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.event_outlined,
                    color: AppColors.azureDark),
                title: const Text('Date de visite'),
                subtitle: Text(_visitedAt == null
                    ? 'Non renseignée'
                    : DateFormat('d MMMM yyyy', 'fr').format(_visitedAt!)),
                trailing: const Icon(Icons.edit_outlined, size: 18),
                onTap: _pickDate,
              ),
              TextField(
                controller: _mileage,
                keyboardType: TextInputType.number,
                inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                decoration: const InputDecoration(
                  labelText: 'Kilométrage constaté',
                  prefixIcon: Icon(Icons.speed),
                ),
              ),
              const SizedBox(height: 20),
              const Text('Points de contrôle',
                  style: TextStyle(
                      fontWeight: FontWeight.w800, color: AppColors.navy)),
              const SizedBox(height: 8),
              for (final type in inspectionItemTypes)
                _ItemRow(
                  label: inspectionItemLabel(type),
                  value: _results[type],
                  onChanged: (v) => setState(() => _results[type] = v),
                ),
              const SizedBox(height: 16),
              TextField(
                controller: _notes,
                maxLines: 3,
                decoration: const InputDecoration(
                  labelText: 'Notes générales',
                  alignLabelWithHint: true,
                ),
              ),
            ],
          ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _saving ? null : _save,
                style:
                    FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                child: _saving
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white))
                    : const Text('Enregistrer'),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _ItemRow extends StatelessWidget {
  const _ItemRow(
      {required this.label, required this.value, required this.onChanged});
  final String label;
  final String? value;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    Widget chip(String v, Color color) {
      final selected = value == v;
      return Padding(
        padding: const EdgeInsets.only(left: 6),
        child: ChoiceChip(
          label: Text(inspectionResultLabel(v)),
          selected: selected,
          labelStyle: TextStyle(
            fontSize: 12,
            color: selected ? Colors.white : color,
            fontWeight: FontWeight.w600,
          ),
          selectedColor: color,
          onSelected: (sel) => onChanged(sel ? v : null),
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style: const TextStyle(
                  fontWeight: FontWeight.w600, color: AppColors.navyDark)),
          const SizedBox(height: 4),
          Row(
            children: [
              chip('Bon', AppColors.success),
              chip('Moyen', AppColors.warning),
              chip('Mauvais', AppColors.error),
            ],
          ),
        ],
      ),
    );
  }
}
