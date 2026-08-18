import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/data/senegal_regions.dart';
import '../../../core/theme/app_colors.dart';
import '../models/vehicle_filters.dart';
import '../providers/vehicle_providers.dart';

/// Resultado del panel de filtros.
class FiltersSheetResult {
  final VehicleFilters filters;
  const FiltersSheetResult(this.filters);
}

/// Panel «Filtres» del Marketplace. Un subconjunto móvil de los filtros de la web.
class FiltersSheet extends ConsumerStatefulWidget {
  const FiltersSheet({super.key, required this.initial});
  final VehicleFilters initial;

  @override
  ConsumerState<FiltersSheet> createState() => _FiltersSheetState();
}

class _FiltersSheetState extends ConsumerState<FiltersSheet> {
  late String? _makeId = widget.initial.makeId;
  late String? _region = widget.initial.region;
  late String? _fuel = widget.initial.fuelType;
  late String? _transmission = widget.initial.transmission;
  late String? _body = widget.initial.bodyType;
  late String? _condition = widget.initial.condition;

  late final _priceFrom =
      TextEditingController(text: widget.initial.priceFrom?.toStringAsFixed(0) ?? '');
  late final _priceTo =
      TextEditingController(text: widget.initial.priceTo?.toStringAsFixed(0) ?? '');
  late final _yearFrom =
      TextEditingController(text: widget.initial.yearFrom?.toString() ?? '');
  late final _yearTo =
      TextEditingController(text: widget.initial.yearTo?.toString() ?? '');
  late final _mileageTo =
      TextEditingController(text: widget.initial.mileageTo?.toString() ?? '');

  @override
  void dispose() {
    _priceFrom.dispose();
    _priceTo.dispose();
    _yearFrom.dispose();
    _yearTo.dispose();
    _mileageTo.dispose();
    super.dispose();
  }

  int? _int(TextEditingController c) {
    final t = c.text.trim();
    return t.isEmpty ? null : int.tryParse(t);
  }

  void _reset() {
    setState(() {
      _makeId = _region = _fuel = _transmission = _body = _condition = null;
      _priceFrom.clear();
      _priceTo.clear();
      _yearFrom.clear();
      _yearTo.clear();
      _mileageTo.clear();
    });
  }

  void _apply() {
    final f = VehicleFilters(
      search: widget.initial.search,
      sortBy: widget.initial.sortBy,
      sortDesc: widget.initial.sortDesc,
      makeId: _makeId,
      region: _region,
      fuelType: _fuel,
      transmission: _transmission,
      bodyType: _body,
      condition: _condition,
      priceFrom: _int(_priceFrom)?.toDouble(),
      priceTo: _int(_priceTo)?.toDouble(),
      yearFrom: _int(_yearFrom),
      yearTo: _int(_yearTo),
      mileageTo: _int(_mileageTo),
    );
    Navigator.pop(context, FiltersSheetResult(f));
  }

  @override
  Widget build(BuildContext context) {
    final makes = ref.watch(makesProvider);

    return DraggableScrollableSheet(
      initialChildSize: 0.85,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) {
        return Container(
          decoration: const BoxDecoration(
            color: AppColors.white,
            borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
          ),
          child: Column(
            children: [
              const SizedBox(height: 10),
              Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: AppColors.silver,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 12, 12, 8),
                child: Row(
                  children: [
                    const Text('Filtres',
                        style: TextStyle(
                            fontSize: 18, fontWeight: FontWeight.w800)),
                    const Spacer(),
                    TextButton(
                        onPressed: _reset,
                        child: const Text('Réinitialiser')),
                  ],
                ),
              ),
              const Divider(height: 1),
              Expanded(
                child: ListView(
                  controller: scrollController,
                  padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
                  children: [
                    _label('Marque'),
                    makes.when(
                      data: (list) => DropdownButtonFormField<String>(
                        initialValue: _makeId,
                        isExpanded: true,
                        decoration:
                            const InputDecoration(hintText: 'Toutes les marques'),
                        items: [
                          const DropdownMenuItem(
                              value: null, child: Text('Toutes les marques')),
                          for (final m in list)
                            DropdownMenuItem(
                                value: m.id, child: Text(m.name)),
                        ],
                        onChanged: (v) => setState(() => _makeId = v),
                      ),
                      loading: () => const LinearProgressIndicator(),
                      error: (_, _) =>
                          const Text('Marques indisponibles'),
                    ),
                    const SizedBox(height: 20),
                    _label('Prix (FCFA)'),
                    _RangeRow(
                      from: _priceFrom,
                      to: _priceTo,
                      fromHint: 'Min',
                      toHint: 'Max',
                    ),
                    const SizedBox(height: 20),
                    _label('Année'),
                    _RangeRow(
                      from: _yearFrom,
                      to: _yearTo,
                      fromHint: 'De',
                      toHint: 'À',
                    ),
                    const SizedBox(height: 20),
                    _label('Kilométrage maximum'),
                    TextField(
                      controller: _mileageTo,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(hintText: 'Ex. 100000'),
                    ),
                    const SizedBox(height: 20),
                    _label('Région'),
                    DropdownButtonFormField<String>(
                      initialValue: _region,
                      isExpanded: true,
                      decoration:
                          const InputDecoration(hintText: 'Toutes les régions'),
                      items: [
                        const DropdownMenuItem(
                            value: null, child: Text('Toutes les régions')),
                        for (final r in senegalRegions)
                          DropdownMenuItem(value: r.name, child: Text(r.name)),
                      ],
                      onChanged: (v) => setState(() => _region = v),
                    ),
                    const SizedBox(height: 20),
                    _label('Carburant'),
                    _Chips(
                      value: _fuel,
                      options: const {
                        'Diesel': 'Diesel',
                        'Essence': 'Essence',
                        'Hybride': 'Hybride',
                        'Electrique': 'Électrique',
                      },
                      onChanged: (v) => setState(() => _fuel = v),
                    ),
                    const SizedBox(height: 20),
                    _label('Boîte de vitesses'),
                    _Chips(
                      value: _transmission,
                      options: const {
                        'Manuel': 'Manuelle',
                        'Automatique': 'Automatique',
                      },
                      onChanged: (v) => setState(() => _transmission = v),
                    ),
                    const SizedBox(height: 20),
                    _label('Carrosserie'),
                    _Chips(
                      value: _body,
                      options: const {
                        'Citadine': 'Citadine',
                        'Berline': 'Berline',
                        'Break': 'Break',
                        'Suv': 'SUV / 4x4',
                        'Coupe': 'Coupé',
                        'Monospace': 'Monospace',
                        'PickUp': 'Pick-up',
                        'Utilitaire': 'Utilitaire',
                      },
                      onChanged: (v) => setState(() => _body = v),
                    ),
                    const SizedBox(height: 20),
                    _label('État'),
                    _Chips(
                      value: _condition,
                      options: const {
                        'New': 'Neuf',
                        'Used': 'Occasion',
                        'Km0': '0 km',
                      },
                      onChanged: (v) => setState(() => _condition = v),
                    ),
                  ],
                ),
              ),
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
                  child: SizedBox(
                    width: double.infinity,
                    child: FilledButton(
                      onPressed: _apply,
                      child: const Text('Voir les résultats'),
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _label(String text) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(text,
            style: const TextStyle(
                fontWeight: FontWeight.w700, color: AppColors.navy)),
      );
}

class _RangeRow extends StatelessWidget {
  const _RangeRow({
    required this.from,
    required this.to,
    required this.fromHint,
    required this.toHint,
  });
  final TextEditingController from;
  final TextEditingController to;
  final String fromHint;
  final String toHint;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: TextField(
            controller: from,
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            decoration: InputDecoration(hintText: fromHint, isDense: true),
          ),
        ),
        const Padding(
          padding: EdgeInsets.symmetric(horizontal: 10),
          child: Text('—', style: TextStyle(color: AppColors.steel)),
        ),
        Expanded(
          child: TextField(
            controller: to,
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            decoration: InputDecoration(hintText: toHint, isDense: true),
          ),
        ),
      ],
    );
  }
}

class _Chips extends StatelessWidget {
  const _Chips({
    required this.value,
    required this.options,
    required this.onChanged,
  });
  final String? value;
  final Map<String, String> options;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final entry in options.entries)
          ChoiceChip(
            label: Text(entry.value),
            selected: value == entry.key,
            onSelected: (sel) => onChanged(sel ? entry.key : null),
          ),
      ],
    );
  }
}
