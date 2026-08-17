import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../negotiations/models/negotiation_enums.dart';
import '../models/admin_negotiation.dart';
import '../providers/admin_providers.dart';
import 'widgets/admin_history.dart';

/// Ficha admin de una negociación: **estructura** (ofertas, cronología), nunca el
/// contenido de los mensajes. Leerlos exige motivo y queda registrado.
class AdminNegotiationDetailScreen extends ConsumerStatefulWidget {
  const AdminNegotiationDetailScreen({super.key, required this.id});
  final String id;

  @override
  ConsumerState<AdminNegotiationDetailScreen> createState() =>
      _AdminNegotiationDetailScreenState();
}

class _AdminNegotiationDetailScreenState
    extends ConsumerState<AdminNegotiationDetailScreen> {
  List<AdminMessage>? _messages;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(adminNegotiationProvider(widget.id));

    return Scaffold(
      appBar: AppBar(title: const Text('Négociation')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminNegotiationProvider(widget.id)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (d) => _body(d),
      ),
    );
  }

  Widget _body(AdminNegotiationDetail d) {
    final n = d.negotiation;
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Text(n.vehicleTitle,
            style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: AppColors.navy)),
        Text('${n.vehicleReference} · ${negotiationStatusLabel(n.status)}',
            style: const TextStyle(color: AppColors.steel)),
        const SizedBox(height: 12),
        AdminSection(
          title: 'Parties',
          child: Column(
            children: [
              AdminRow('Acheteur', n.buyerName),
              AdminRow('Vendeur', n.sellerName),
              if (n.contractReference != null)
                AdminRow('Contrat', n.contractReference!),
            ],
          ),
        ),
        AdminSection(
          title: 'Offres (${d.offers.length})',
          child: d.offers.isEmpty
              ? const Text('Aucune offre.',
                  style: TextStyle(color: AppColors.steel))
              : Column(
                  children: [
                    for (final o in d.offers)
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        child: Row(
                          children: [
                            Icon(
                                o.fromBuyer
                                    ? Icons.arrow_upward
                                    : Icons.arrow_downward,
                                size: 16,
                                color: AppColors.steel),
                            const SizedBox(width: 8),
                            Expanded(
                                child: Text(
                                    '${fcfa(o.amount)} · ${offerStatusLabel(o.status)}',
                                    style: const TextStyle(fontSize: 13))),
                            Text(
                                DateFormat('d MMM', 'fr')
                                    .format(o.createdAt.toLocal()),
                                style: const TextStyle(
                                    fontSize: 11, color: AppColors.steel)),
                          ],
                        ),
                      ),
                  ],
                ),
        ),
        AdminSection(
          title: 'Chronologie',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final e in d.timeline)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 3),
                  child: Text(
                    '• ${adminTimelineLabel(e.type, amountLabel: e.amount != null ? fcfa(e.amount) : null)} — ${DateFormat('d MMM yyyy', 'fr').format(e.createdAt.toLocal())}',
                    style: const TextStyle(fontSize: 13),
                  ),
                ),
            ],
          ),
        ),
        _ContentSection(
          messageCount: n.messagesCount,
          messages: _messages,
          onRead: () => _readContent(d),
        ),
        AdminSection(
          title: 'Accès enregistrés',
          child: AdminActionsHistory(actions: d.actions),
        ),
      ],
    );
  }

  Future<void> _readContent(AdminNegotiationDetail d) async {
    final access = await _askContentAccess();
    if (access == null) return;
    try {
      final messages = await ref.read(adminRepositoryProvider).accessNegotiationContent(
            widget.id,
            reason: access.reason,
            details: access.details,
          );
      // El acceso queda registrado: refrescamos para verlo en el historial.
      ref.invalidate(adminNegotiationProvider(widget.id));
      if (!mounted) return;
      setState(() => _messages = messages);
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Accès impossible.')),
        );
      }
    }
  }

  Future<({String reason, String? details})?> _askContentAccess() {
    String reason = contentAccessReasonValues.first;
    final details = TextEditingController();
    return showDialog<({String reason, String? details})>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setStateDialog) => AlertDialog(
          title: const Text('Lire le contenu'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'La lecture des messages exige un motif et sera enregistrée.',
                style: TextStyle(fontSize: 12, color: AppColors.steel),
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: reason,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Motif'),
                items: [
                  for (final r in contentAccessReasonValues)
                    DropdownMenuItem(
                        value: r, child: Text(contentAccessReasonLabel(r))),
                ],
                onChanged: (v) =>
                    setStateDialog(() => reason = v ?? reason),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: details,
                decoration:
                    const InputDecoration(labelText: 'Détails (facultatif)'),
              ),
            ],
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Annuler')),
            FilledButton(
              onPressed: () => Navigator.pop(ctx, (
                reason: reason,
                details:
                    details.text.trim().isEmpty ? null : details.text.trim(),
              )),
              child: const Text('Accéder'),
            ),
          ],
        ),
      ),
    );
  }
}

class _ContentSection extends StatelessWidget {
  const _ContentSection(
      {required this.messageCount,
      required this.messages,
      required this.onRead});
  final int messageCount;
  final List<AdminMessage>? messages;
  final VoidCallback onRead;

  @override
  Widget build(BuildContext context) {
    return AdminSection(
      title: 'Contenu ($messageCount message(s))',
      child: messages == null
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Le contenu des messages est masqué. Sa lecture est justifiée et tracée.',
                  style: TextStyle(fontSize: 12, color: AppColors.steel),
                ),
                const SizedBox(height: 10),
                OutlinedButton.icon(
                  onPressed: onRead,
                  icon: const Icon(Icons.lock_open_outlined),
                  label: const Text('Lire le contenu (avec motif)'),
                ),
              ],
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (final m in messages!)
                  Container(
                    margin: const EdgeInsets.symmetric(vertical: 3),
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: AppColors.frost,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(m.fromBuyer ? 'Acheteur' : 'Vendeur',
                            style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                                color: AppColors.steel)),
                        Text(m.body, style: const TextStyle(fontSize: 13)),
                      ],
                    ),
                  ),
              ],
            ),
    );
  }
}
