import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../models/transparency.dart';
import '../providers/garage_providers.dart';

/// «Transparence du véhicule»: el vendedor elige, casilla a casilla, qué parte de
/// su historial privado se enseña en el anuncio. Nada se comparte sin marcarlo.
class TransparencyScreen extends ConsumerWidget {
  const TransparencyScreen({super.key, required this.listedVehicleId});
  final String listedVehicleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(transparencyProvider(listedVehicleId));

    return Scaffold(
      appBar: AppBar(title: const Text('Transparence')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () =>
                ref.invalidate(transparencyProvider(listedVehicleId)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (settings) =>
            _Form(listedVehicleId: listedVehicleId, settings: settings),
      ),
    );
  }
}

class _Form extends ConsumerStatefulWidget {
  const _Form({required this.listedVehicleId, required this.settings});
  final String listedVehicleId;
  final TransparencySettings settings;

  @override
  ConsumerState<_Form> createState() => _FormState();
}

class _FormState extends ConsumerState<_Form> {
  late bool _showHistory = widget.settings.showMaintenanceHistory;
  late bool _showDetails = widget.settings.showMaintenanceDetails;
  late bool _showMileage = widget.settings.showMileageEvolution;
  late final List<TransparencyRecord> _records = widget.settings.records
      .map((r) => TransparencyRecord(
            maintenanceRecordId: r.maintenanceRecordId,
            type: r.type,
            performedAt: r.performedAt,
            mileage: r.mileage,
            description: r.description,
            hasInvoice: r.hasInvoice,
            shared: r.shared,
            shareInvoice: r.shareInvoice,
          ))
      .toList();
  bool _saving = false;

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      await ref.read(garageRepositoryProvider).saveTransparency(
            widget.listedVehicleId,
            showMaintenanceHistory: _showHistory,
            showMaintenanceDetails: _showDetails,
            showMileageEvolution: _showMileage,
            records: _records,
          );
      ref.invalidate(transparencyProvider(widget.listedVehicleId));
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Préférences enregistrées.')),
      );
      Navigator.of(context).pop();
    } catch (_) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Enregistrement impossible.')),
      );
    }
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
                child: const Text(
                  'Rien n’est partagé tant que vous ne le cochez pas. Partager une intervention ne partage pas sa facture : ce sont deux cases.',
                  style: TextStyle(fontSize: 12, color: AppColors.steel),
                ),
              ),
              const SizedBox(height: 8),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                value: _showHistory,
                onChanged: (v) => setState(() => _showHistory = v),
                title: const Text('Afficher l’historique d’entretien'),
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                value: _showDetails,
                onChanged: _showHistory
                    ? (v) => setState(() => _showDetails = v)
                    : null,
                title: const Text('Afficher le détail des interventions'),
                subtitle: const Text('Type, kilométrage et description'),
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                value: _showMileage,
                onChanged: (v) => setState(() => _showMileage = v),
                title: const Text('Afficher l’évolution du kilométrage'),
              ),
              if (_records.isNotEmpty) ...[
                const SizedBox(height: 12),
                const Text('Interventions à partager',
                    style: TextStyle(
                        fontWeight: FontWeight.w800, color: AppColors.navy)),
                const SizedBox(height: 8),
                for (final r in _records) _RecordCard(record: r, onChanged: () => setState(() {})),
              ],
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
            ),
          ),
        ),
      ],
    );
  }
}

class _RecordCard extends StatelessWidget {
  const _RecordCard({required this.record, required this.onChanged});
  final TransparencyRecord record;
  final VoidCallback onChanged;

  @override
  Widget build(BuildContext context) {
    final r = record;
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 4, 12, 4),
        child: Column(
          children: [
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              value: r.shared,
              onChanged: (v) {
                r.shared = v ?? false;
                if (!r.shared) r.shareInvoice = false;
                onChanged();
              },
              title: Text(r.typeLabel,
                  style: const TextStyle(fontWeight: FontWeight.w700)),
              subtitle: Text([
                DateFormat('d MMM yyyy', 'fr').format(r.performedAt.toLocal()),
                if (r.mileage != null)
                  '${fcfa(r.mileage, withSuffix: false)} km',
              ].join(' · ')),
            ),
            if (r.hasInvoice)
              CheckboxListTile(
                contentPadding: const EdgeInsets.only(left: 16),
                value: r.shareInvoice,
                onChanged: r.shared
                    ? (v) {
                        r.shareInvoice = v ?? false;
                        onChanged();
                      }
                    : null,
                title: const Text('Partager aussi la facture',
                    style: TextStyle(fontSize: 13)),
              ),
          ],
        ),
      ),
    );
  }
}
