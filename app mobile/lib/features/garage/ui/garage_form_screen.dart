import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../vehicles/providers/vehicle_providers.dart';
import '../models/garage_models.dart';
import '../providers/garage_providers.dart';

/// Alta / edición de un vehículo de Mon Garage. `id` nulo = alta.
class GarageFormScreen extends ConsumerWidget {
  const GarageFormScreen({super.key, this.id});
  final String? id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (id == null) {
      return const _FormScaffold(title: 'Ajouter un véhicule', initial: null);
    }
    final async = ref.watch(garageVehicleProvider(id!));
    return async.when(
      loading: () => const Scaffold(
          body: Center(child: CircularProgressIndicator())),
      error: (_, _) => Scaffold(
        appBar: AppBar(),
        body: const Center(child: Text('Véhicule introuvable')),
      ),
      data: (v) => _FormScaffold(title: 'Modifier', initial: v),
    );
  }
}

class _FormScaffold extends ConsumerStatefulWidget {
  const _FormScaffold({required this.title, required this.initial});
  final String title;
  final GarageVehicleDetail? initial;

  @override
  ConsumerState<_FormScaffold> createState() => _FormScaffoldState();
}

class _FormScaffoldState extends ConsumerState<_FormScaffold> {
  final _formKey = GlobalKey<FormState>();
  late String? _makeId = widget.initial?.makeId;
  late String? _fuel = widget.initial?.fuelType;
  late String? _transmission = widget.initial?.transmission;
  late String? _body = widget.initial?.bodyType;
  late DateTime? _purchaseDate = widget.initial?.purchaseDate;

  late final _version = TextEditingController(text: widget.initial?.version ?? '');
  late final _year =
      TextEditingController(text: widget.initial?.year.toString() ?? '');
  late final _mileage =
      TextEditingController(text: widget.initial?.mileage?.toString() ?? '');
  late final _power =
      TextEditingController(text: widget.initial?.powerCv?.toString() ?? '');
  late final _displacement = TextEditingController(
      text: widget.initial?.engineDisplacementCc?.toString() ?? '');
  late final _color = TextEditingController(text: widget.initial?.color ?? '');
  late final _plate =
      TextEditingController(text: widget.initial?.registrationPlate ?? '');
  late final _vin = TextEditingController(text: widget.initial?.vin ?? '');
  late final _price = TextEditingController(
      text: widget.initial?.purchasePrice?.round().toString() ?? '');

  bool _saving = false;

  @override
  void dispose() {
    for (final c in [
      _version, _year, _mileage, _power, _displacement, _color, _plate,
      _vin, _price,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  int? _int(TextEditingController c) =>
      c.text.trim().isEmpty ? null : int.tryParse(c.text.trim());

  String? _str(TextEditingController c) =>
      c.text.trim().isEmpty ? null : c.text.trim();

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_makeId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Choisissez une marque.')),
      );
      return;
    }
    setState(() => _saving = true);
    final body = {
      'makeId': _makeId,
      'version': _str(_version),
      'year': int.parse(_year.text.trim()),
      'mileage': _int(_mileage),
      'fuelType': _fuel,
      'transmission': _transmission,
      'bodyType': _body,
      'powerCv': _int(_power),
      'engineDisplacementCc': _int(_displacement),
      'color': _str(_color),
      'registrationPlate': _str(_plate),
      'vin': _str(_vin),
      'purchaseDate': _purchaseDate?.toUtc().toIso8601String(),
      'purchasePrice': _int(_price),
    };
    try {
      final repo = ref.read(garageRepositoryProvider);
      if (widget.initial == null) {
        await repo.createVehicle(body);
      } else {
        await repo.updateVehicle(widget.initial!.id, body);
        ref.invalidate(garageVehicleProvider(widget.initial!.id));
      }
      ref.invalidate(garageSummaryProvider);
      if (!mounted) return;
      context.pop();
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
      initialDate: _purchaseDate ?? now,
      firstDate: DateTime(now.year - 40),
      lastDate: now,
    );
    if (picked != null) setState(() => _purchaseDate = picked);
  }

  @override
  Widget build(BuildContext context) {
    final makes = ref.watch(makesProvider);

    return Scaffold(
      appBar: AppBar(title: Text(widget.title)),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
          children: [
            makes.when(
              data: (list) => DropdownButtonFormField<String>(
                initialValue: _makeId,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Marque *'),
                items: [
                  for (final m in list)
                    DropdownMenuItem(value: m.id, child: Text(m.name)),
                ],
                onChanged: (v) => setState(() => _makeId = v),
              ),
              loading: () => const LinearProgressIndicator(),
              error: (_, _) => const Text('Marques indisponibles'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _version,
              decoration:
                  const InputDecoration(labelText: 'Version / finition'),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _year,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(labelText: 'Année *'),
                    validator: (v) {
                      final y = int.tryParse((v ?? '').trim());
                      if (y == null || y < 1950 || y > DateTime.now().year + 1) {
                        return 'Année invalide';
                      }
                      return null;
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _mileage,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(labelText: 'Km'),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            _dropdown('Carburant', _fuel, const {
              'Diesel': 'Diesel',
              'Essence': 'Essence',
              'Hybride': 'Hybride',
              'HybrideRechargeable': 'Hybride rechargeable',
              'Electrique': 'Électrique',
              'Autre': 'Autre',
            }, (v) => setState(() => _fuel = v)),
            const SizedBox(height: 12),
            _dropdown('Boîte de vitesses', _transmission, const {
              'Manuel': 'Manuelle',
              'Automatique': 'Automatique',
            }, (v) => setState(() => _transmission = v)),
            const SizedBox(height: 12),
            _dropdown('Carrosserie', _body, const {
              'Citadine': 'Citadine',
              'Berline': 'Berline',
              'Break': 'Break',
              'Suv': 'SUV / 4x4',
              'Coupe': 'Coupé',
              'Cabriolet': 'Cabriolet',
              'Monospace': 'Monospace',
              'PickUp': 'Pick-up',
              'Utilitaire': 'Utilitaire',
              'Autre': 'Autre',
            }, (v) => setState(() => _body = v)),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _power,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(labelText: 'Puissance (ch)'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _displacement,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(labelText: 'Cylindrée (cm³)'),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _color,
              decoration: const InputDecoration(labelText: 'Couleur'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _plate,
              textCapitalization: TextCapitalization.characters,
              decoration: const InputDecoration(labelText: 'Immatriculation'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _vin,
              textCapitalization: TextCapitalization.characters,
              decoration:
                  const InputDecoration(labelText: 'Numéro de châssis (VIN)'),
            ),
            const SizedBox(height: 20),
            const Text('Achat (facultatif)',
                style: TextStyle(
                    fontWeight: FontWeight.w800, color: AppColors.navy)),
            const SizedBox(height: 8),
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.event_outlined),
              title: const Text('Date d’achat'),
              subtitle: Text(_purchaseDate == null
                  ? 'Non renseignée'
                  : DateFormat('d MMMM yyyy', 'fr').format(_purchaseDate!)),
              trailing: const Icon(Icons.edit_outlined, size: 18),
              onTap: _pickDate,
            ),
            TextFormField(
              controller: _price,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              decoration: const InputDecoration(labelText: 'Prix d’achat (FCFA)'),
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _saving ? null : _save,
              style:
                  FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
              child: _saving
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: Colors.white))
                  : Text(widget.initial == null ? 'Ajouter' : 'Enregistrer'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _dropdown(String label, String? value, Map<String, String> options,
      ValueChanged<String?> onChanged) {
    return DropdownButtonFormField<String>(
      initialValue: value,
      isExpanded: true,
      decoration: InputDecoration(labelText: label),
      items: [
        const DropdownMenuItem(value: null, child: Text('—')),
        for (final e in options.entries)
          DropdownMenuItem(value: e.key, child: Text(e.value)),
      ],
      onChanged: onChanged,
    );
  }
}
