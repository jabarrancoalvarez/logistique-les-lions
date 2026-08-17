import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../../core/config/api_config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../../core/util/pdf_opener.dart';
import '../data/negotiation_repository.dart';
import '../models/contract.dart';
import '../providers/negotiation_providers.dart';

/// Pestaña «Contrat» de una negociación: ver, redactar, enviar, valider,
/// descargar el PDF y abrir la página de vérification.
class ContractScreen extends ConsumerStatefulWidget {
  const ContractScreen({super.key, required this.negotiationId});
  final String negotiationId;

  @override
  ConsumerState<ContractScreen> createState() => _ContractScreenState();
}

class _ContractScreenState extends ConsumerState<ContractScreen> {
  bool _editing = false;
  bool _busy = false;

  Future<void> _run(Future<void> Function() action, {String? ok}) async {
    setState(() => _busy = true);
    try {
      await action();
      ref.invalidate(contractTabProvider(widget.negotiationId));
      if (!mounted) return;
      setState(() {
        _busy = false;
        _editing = false;
      });
      if (ok != null) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(ok)));
      }
    } catch (_) {
      if (!mounted) return;
      setState(() => _busy = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Action impossible. Réessayez.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(contractTabProvider(widget.negotiationId));

    return Scaffold(
      appBar: AppBar(title: const Text('Contrat')),
      body: Stack(
        children: [
          async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (_, _) => Center(
              child: FilledButton(
                onPressed: () =>
                    ref.invalidate(contractTabProvider(widget.negotiationId)),
                child: const Text('Réessayer'),
              ),
            ),
            data: (tab) {
              final contract = tab.contract;
              if (contract != null && !_editing) {
                return _ContractView(
                  contract: contract,
                  onEdit: () => setState(() => _editing = true),
                  onSend: () => _run(
                      () => _repo.sendContract(contract.id),
                      ok: 'Contrat envoyé.'),
                  onValidate: () => _confirmValidate(contract),
                  onRequestChanges: () => _requestChanges(contract),
                  onCancel: () => _confirmCancel(contract),
                  onDownload: () => _download(contract),
                  onVerification: () => _openVerification(contract),
                );
              }
              if (contract == null && !tab.canCreate) {
                return const _EmptyContract();
              }
              // Crear o editar.
              return _ContractForm(
                prefill: tab.prefill,
                existing: contract,
                onCancel: contract == null
                    ? null
                    : () => setState(() => _editing = false),
                onSubmit: (body) => contract == null
                    ? _run(
                        () => _repo
                            .createContract(widget.negotiationId, body)
                            .then((_) {}),
                        ok: 'Contrat créé.')
                    : _run(() => _repo.updateContract(contract.id, body),
                        ok: 'Contrat mis à jour.'),
              );
            },
          ),
          if (_busy)
            const Positioned.fill(
              child: ColoredBox(
                color: Color(0x66000000),
                child: Center(child: CircularProgressIndicator()),
              ),
            ),
        ],
      ),
    );
  }

  NegotiationRepository get _repo =>
      ref.read(negotiationRepositoryProvider);

  Future<void> _confirmValidate(Contract c) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Valider le contrat ?'),
        content: const Text(
            'La validation confirme la vente. Elle est définitive et génère le code de vérification.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Annuler')),
          FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Valider')),
        ],
      ),
    );
    if (ok == true) {
      await _run(() => _repo.validateContract(c.id),
          ok: 'Vente validée et vérifiée ✓');
    }
  }

  Future<void> _confirmCancel(Contract c) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Annuler le contrat ?'),
        content: const Text('Le contrat sera annulé. Cette action est irréversible.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Retour')),
          FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.error),
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Annuler le contrat')),
        ],
      ),
    );
    if (ok == true) {
      await _run(() => _repo.cancelContract(c.id), ok: 'Contrat annulé.');
    }
  }

  Future<void> _requestChanges(Contract c) async {
    final ctrl = TextEditingController();
    final notes = await showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Demander une modification'),
        content: TextField(
          controller: ctrl,
          maxLines: 3,
          autofocus: true,
          decoration:
              const InputDecoration(hintText: 'Que faut-il corriger ?'),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Annuler')),
          FilledButton(
              onPressed: () => Navigator.pop(context, ctrl.text.trim()),
              child: const Text('Envoyer')),
        ],
      ),
    );
    if (notes != null && notes.isNotEmpty) {
      await _run(() => _repo.requestContractChanges(c.id, notes),
          ok: 'Demande envoyée.');
    }
  }

  Future<void> _download(Contract c) async {
    setState(() => _busy = true);
    try {
      final bytes = await _repo.downloadContractPdf(c.id);
      final opened =
          await saveAndOpenPdf(bytes, 'contrat-${c.publicReference}.pdf');
      if (!mounted) return;
      setState(() => _busy = false);
      if (!opened) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('PDF téléchargé. Aucun lecteur trouvé.')),
        );
      }
    } catch (_) {
      if (!mounted) return;
      setState(() => _busy = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Téléchargement impossible.')),
      );
    }
  }

  Future<void> _openVerification(Contract c) async {
    if (c.verificationCode == null) return;
    final uri = Uri.parse('${ApiConfig.webUrl}/verification/${c.verificationCode}');
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Impossible d’ouvrir la page.')),
      );
    }
  }
}

class _ContractView extends StatelessWidget {
  const _ContractView({
    required this.contract,
    required this.onEdit,
    required this.onSend,
    required this.onValidate,
    required this.onRequestChanges,
    required this.onCancel,
    required this.onDownload,
    required this.onVerification,
  });
  final Contract contract;
  final VoidCallback onEdit;
  final VoidCallback onSend;
  final VoidCallback onValidate;
  final VoidCallback onRequestChanges;
  final VoidCallback onCancel;
  final VoidCallback onDownload;
  final VoidCallback onVerification;

  @override
  Widget build(BuildContext context) {
    final c = contract;
    final vehicle = [c.vehicleMake, c.vehicleModel, c.vehicleVersion]
        .where((e) => e != null && e.isNotEmpty)
        .join(' ');

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        _StatusHeader(contract: c),
        const SizedBox(height: 16),
        _Section(title: 'Véhicule', rows: [
          ('Modèle', '$vehicle ${c.vehicleYear}'),
          if (c.vehicleMileage != null)
            ('Kilométrage', '${fcfa(c.vehicleMileage, withSuffix: false)} km'),
          if (c.registrationPlate != null) ('Immatriculation', c.registrationPlate!),
          if (c.vehicleVin != null) ('VIN', c.vehicleVin!),
          ('Référence', c.vehicleReference),
        ]),
        _Section(title: 'Prix et date', rows: [
          ('Prix convenu', fcfa(c.agreedPrice)),
          ('Date de vente',
              DateFormat('d MMMM yyyy', 'fr').format(c.saleDate.toLocal())),
        ]),
        _Section(title: 'Vendeur', rows: [
          ('Nom', c.sellerLegalName),
          if (c.sellerIdDocument != null) ('Pièce d’identité', c.sellerIdDocument!),
          if (c.sellerAddress != null) ('Adresse', c.sellerAddress!),
        ]),
        _Section(title: 'Acheteur', rows: [
          ('Nom', c.buyerLegalName),
          if (c.buyerIdDocument != null) ('Pièce d’identité', c.buyerIdDocument!),
          if (c.buyerAddress != null) ('Adresse', c.buyerAddress!),
        ]),
        if (c.changeRequestNotes != null && c.changeRequestNotes!.isNotEmpty)
          Container(
            margin: const EdgeInsets.only(top: 8),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.warning.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.edit_note, color: AppColors.warning, size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Text('Modification demandée : ${c.changeRequestNotes}',
                      style: const TextStyle(fontSize: 13)),
                ),
              ],
            ),
          ),
        const SizedBox(height: 20),
        ..._actions(context),
      ],
    );
  }

  List<Widget> _actions(BuildContext context) {
    final c = contract;
    final buttons = <Widget>[];

    if (c.canValidate) {
      buttons.add(FilledButton.icon(
        onPressed: onValidate,
        icon: const Icon(Icons.verified_outlined),
        label: const Text('Valider le contrat'),
        style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
      ));
    }
    if (c.canSend) {
      buttons.add(FilledButton.icon(
        onPressed: onSend,
        icon: const Icon(Icons.send_outlined),
        label: const Text('Envoyer à l’autre partie'),
        style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
      ));
    }
    if (c.canDownloadPdf) {
      buttons.add(FilledButton.icon(
        onPressed: onDownload,
        icon: const Icon(Icons.picture_as_pdf_outlined),
        label: const Text('Télécharger le PDF'),
        style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
      ));
    }
    if (c.verificationCode != null) {
      buttons.add(OutlinedButton.icon(
        onPressed: onVerification,
        icon: const Icon(Icons.qr_code_2),
        label: const Text('Voir la vérification'),
        style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
      ));
    }
    if (c.canRequestChanges) {
      buttons.add(OutlinedButton.icon(
        onPressed: onRequestChanges,
        icon: const Icon(Icons.edit_outlined),
        label: const Text('Demander une modification'),
        style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
      ));
    }
    if (c.canEdit) {
      buttons.add(OutlinedButton.icon(
        onPressed: onEdit,
        icon: const Icon(Icons.tune),
        label: const Text('Modifier'),
        style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
      ));
    }
    if (c.canCancel) {
      buttons.add(TextButton.icon(
        onPressed: onCancel,
        icon: const Icon(Icons.cancel_outlined, color: AppColors.error),
        label: const Text('Annuler le contrat',
            style: TextStyle(color: AppColors.error)),
      ));
    }

    return [
      for (var i = 0; i < buttons.length; i++) ...[
        buttons[i],
        if (i != buttons.length - 1) const SizedBox(height: 10),
      ],
    ];
  }
}

class _StatusHeader extends StatelessWidget {
  const _StatusHeader({required this.contract});
  final Contract contract;

  @override
  Widget build(BuildContext context) {
    final validated = contract.status == 'Valide';
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [AppColors.navy, AppColors.navyLight],
        ),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          Icon(validated ? Icons.verified : Icons.description_outlined,
              color: AppColors.white, size: 28),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Contrat ${contract.publicReference}',
                    style: const TextStyle(
                        color: AppColors.white,
                        fontWeight: FontWeight.w800,
                        fontSize: 16)),
                Text(contractStatusLabel(contract.status),
                    style: TextStyle(
                        color: AppColors.white.withValues(alpha: 0.85),
                        fontSize: 13)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.rows});
  final String title;
  final List<(String, String)> rows;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  color: AppColors.navy,
                  fontSize: 15)),
          const SizedBox(height: 8),
          for (final (label, value) in rows)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 3),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(
                    width: 120,
                    child: Text(label,
                        style: const TextStyle(
                            color: AppColors.steel, fontSize: 13)),
                  ),
                  Expanded(
                    child: Text(value,
                        style: const TextStyle(
                            fontWeight: FontWeight.w600,
                            color: AppColors.navyDark,
                            fontSize: 13)),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _EmptyContract extends StatelessWidget {
  const _EmptyContract();
  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.description_outlined, size: 56, color: AppColors.silver),
            SizedBox(height: 16),
            Text('Pas encore de contrat',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            SizedBox(height: 6),
            Text(
              'Le contrat pourra être créé une fois un accord trouvé dans la négociation.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.steel),
            ),
          ],
        ),
      ),
    );
  }
}

/// Formulario de creación / edición del contrato.
class _ContractForm extends StatefulWidget {
  const _ContractForm({
    required this.prefill,
    required this.existing,
    required this.onSubmit,
    this.onCancel,
  });
  final ContractPrefill prefill;
  final Contract? existing;
  final ValueChanged<Map<String, dynamic>> onSubmit;
  final VoidCallback? onCancel;

  @override
  State<_ContractForm> createState() => _ContractFormState();
}

class _ContractFormState extends State<_ContractForm> {
  final _formKey = GlobalKey<FormState>();
  late final _price = TextEditingController(
      text: (widget.existing?.agreedPrice ?? widget.prefill.suggestedPrice)
          .round()
          .toString());
  late final _plate =
      TextEditingController(text: widget.existing?.registrationPlate ?? '');
  late final _sellerName = TextEditingController(
      text: widget.existing?.sellerLegalName ?? widget.prefill.sellerLegalName);
  late final _sellerId =
      TextEditingController(text: widget.existing?.sellerIdDocument ?? '');
  late final _sellerAddr =
      TextEditingController(text: widget.existing?.sellerAddress ?? '');
  late final _buyerName = TextEditingController(
      text: widget.existing?.buyerLegalName ?? widget.prefill.buyerLegalName);
  late final _buyerId =
      TextEditingController(text: widget.existing?.buyerIdDocument ?? '');
  late final _buyerAddr =
      TextEditingController(text: widget.existing?.buyerAddress ?? '');

  @override
  void dispose() {
    for (final c in [
      _price, _plate, _sellerName, _sellerId, _sellerAddr,
      _buyerName, _buyerId, _buyerAddr,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  String? _required(String? v) =>
      (v == null || v.trim().isEmpty) ? 'Champ requis' : null;

  void _submit() {
    if (!_formKey.currentState!.validate()) return;
    final price = num.tryParse(_price.text.trim());
    if (price == null || price <= 0) return;
    String? opt(TextEditingController c) =>
        c.text.trim().isEmpty ? null : c.text.trim();

    widget.onSubmit({
      'agreedPrice': price,
      'registrationPlate': opt(_plate),
      'sellerLegalName': _sellerName.text.trim(),
      'sellerIdDocument': opt(_sellerId),
      'sellerAddress': opt(_sellerAddr),
      'buyerLegalName': _buyerName.text.trim(),
      'buyerIdDocument': opt(_buyerId),
      'buyerAddress': opt(_buyerAddr),
    });
  }

  @override
  Widget build(BuildContext context) {
    final p = widget.prefill;
    final vehicle = [p.vehicleMake, p.vehicleModel, p.vehicleVersion]
        .where((e) => e != null && e.isNotEmpty)
        .join(' ');

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
        children: [
          Text('$vehicle ${p.vehicleYear} · ${p.vehicleReference}',
              style: const TextStyle(color: AppColors.steel)),
          const SizedBox(height: 16),
          TextFormField(
            controller: _price,
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            decoration: const InputDecoration(
                labelText: 'Prix convenu (FCFA)',
                prefixIcon: Icon(Icons.payments_outlined)),
            validator: _required,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _plate,
            decoration: const InputDecoration(
                labelText: 'Immatriculation (facultatif)'),
          ),
          const _FormSection('Vendeur'),
          TextFormField(
            controller: _sellerName,
            decoration: const InputDecoration(labelText: 'Nom légal'),
            validator: _required,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _sellerId,
            decoration: const InputDecoration(
                labelText: 'Pièce d’identité (facultatif)'),
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _sellerAddr,
            decoration:
                const InputDecoration(labelText: 'Adresse (facultatif)'),
          ),
          const _FormSection('Acheteur'),
          TextFormField(
            controller: _buyerName,
            decoration: const InputDecoration(labelText: 'Nom légal'),
            validator: _required,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _buyerId,
            decoration: const InputDecoration(
                labelText: 'Pièce d’identité (facultatif)'),
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _buyerAddr,
            decoration:
                const InputDecoration(labelText: 'Adresse (facultatif)'),
          ),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: _submit,
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
            child: Text(widget.existing == null
                ? 'Créer le contrat'
                : 'Enregistrer les modifications'),
          ),
          if (widget.onCancel != null) ...[
            const SizedBox(height: 8),
            TextButton(
                onPressed: widget.onCancel, child: const Text('Annuler')),
          ],
        ],
      ),
    );
  }
}

class _FormSection extends StatelessWidget {
  const _FormSection(this.title);
  final String title;
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 20, bottom: 8),
      child: Text(title,
          style: const TextStyle(
              fontWeight: FontWeight.w800, color: AppColors.navy, fontSize: 15)),
    );
  }
}
