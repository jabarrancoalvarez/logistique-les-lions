import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../vehicles/models/vehicle_enums.dart';
import '../models/admin_enums.dart';
import '../models/admin_listing.dart';
import '../providers/admin_providers.dart';
import 'reason_dialog.dart';
import 'widgets/admin_history.dart';

class AdminListingDetailScreen extends ConsumerWidget {
  const AdminListingDetailScreen({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminListingProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Annonce')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminListingProvider(id)),
            child: const Text('Réessayer'),
          ),
        ),
        data: (d) => _Body(detail: d),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.detail});
  final AdminListingDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = detail.listing;

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Text(l.title,
            style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: AppColors.navy)),
        const SizedBox(height: 4),
        Text('${l.publicReference} · ${statusLabel(l.status)}',
            style: const TextStyle(color: AppColors.steel)),
        const SizedBox(height: 8),
        Wrap(spacing: 8, children: [
          if (l.hiddenByAdmin)
            const _Flag(text: 'Masquée', color: AppColors.error),
          if (l.flaggedForReview)
            const _Flag(text: 'À réviser', color: AppColors.warning),
          if (l.openReports > 0)
            _Flag(text: '${l.openReports} signalement(s)', color: AppColors.error),
        ]),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: () => context.push('/vehicules/${l.slug}'),
          icon: const Icon(Icons.open_in_new, size: 18),
          label: const Text('Voir l’annonce publique'),
        ),
        const SizedBox(height: 12),
        AdminSection(
          title: 'Données',
          child: Column(
            children: [
              AdminRow('Prix', fcfa(l.price)),
              AdminRow('Vendeur', l.sellerName),
              AdminRow('Téléphone', detail.sellerPhone),
              AdminRow('Type', accountTypeLabel(l.sellerAccountType)),
              if (l.city != null) AdminRow('Ville', l.city!),
              AdminRow('Vues', '${l.viewsCount}'),
              AdminRow('Favoris', '${l.favoritesCount}'),
              AdminRow('Contacts', '${detail.contactsCount}'),
              AdminRow('Négociations', '${detail.negotiationsCount}'),
              AdminRow('Offres reçues', '${detail.offersReceived}'),
              AdminRow('Qualité', '${l.qualityScore} / 100'),
            ],
          ),
        ),
        AdminSection(
          title: 'Modération',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'L’administrateur ne modifie jamais l’information commerciale : il masque, marque ou demande une correction. Tout exige un motif.',
                style: TextStyle(fontSize: 12, color: AppColors.steel),
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final action in _availableActions(l))
                    OutlinedButton(
                      onPressed: () => _doAction(context, ref, l.id, action),
                      child: Text(listingActionLabel(action)),
                    ),
                ],
              ),
              const SizedBox(height: 8),
              FilledButton.icon(
                onPressed: () => _requestCorrection(context, ref, l.id),
                icon: const Icon(Icons.edit_note),
                label: const Text('Demander une correction'),
              ),
            ],
          ),
        ),
        AdminSection(
          title: 'Historique',
          child: AdminActionsHistory(
              actions: detail.actions, notes: detail.notes),
        ),
      ],
    );
  }

  List<String> _availableActions(AdminListingRow l) {
    return [
      if (!l.hiddenByAdmin) 'Hide' else 'Reactivate',
      if (!l.flaggedForReview) 'Flag' else 'Unflag',
      'Archive',
      'Delete',
    ];
  }

  Future<void> _doAction(
      BuildContext context, WidgetRef ref, String id, String action) async {
    final reason = await askReason(
      context,
      title: '${listingActionLabel(action)} cette annonce ?',
      confirmLabel: listingActionLabel(action),
      destructive: action == 'Delete' || action == 'Hide' || action == 'Archive',
    );
    if (reason == null) return;
    try {
      await ref
          .read(adminRepositoryProvider)
          .listingAction(id, action: action, reason: reason);
    } catch (_) {
      if (context.mounted) _snack(context, 'Action impossible.');
      return;
    }
    ref.invalidate(adminListingProvider(id));
    if (!context.mounted) return;
    if (action == 'Delete') context.pop();
    _snack(context, 'Action appliquée.');
  }

  Future<void> _requestCorrection(
      BuildContext context, WidgetRef ref, String id) async {
    final message = await askReason(
      context,
      title: 'Demander une correction',
      hint: 'Que doit corriger le vendeur ?',
      confirmLabel: 'Envoyer',
    );
    if (message == null) return;
    try {
      await ref.read(adminRepositoryProvider).requestCorrection(id, message);
    } catch (_) {
      if (context.mounted) _snack(context, 'Action impossible.');
      return;
    }
    ref.invalidate(adminListingProvider(id));
    if (context.mounted) _snack(context, 'Demande envoyée au vendeur.');
  }

  void _snack(BuildContext context, String m) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));
  }
}

class _Flag extends StatelessWidget {
  const _Flag({required this.text, required this.color});
  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(text,
          style: TextStyle(
              fontSize: 11, fontWeight: FontWeight.w700, color: color)),
    );
  }
}
