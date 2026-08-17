import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../negotiations/models/contract.dart';
import '../models/admin_contract.dart';
import '../providers/admin_providers.dart';
import 'reason_dialog.dart';
import 'widgets/admin_history.dart';

class AdminContractDetailScreen extends ConsumerWidget {
  const AdminContractDetailScreen({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminContractProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Contrat')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminContractProvider(id)),
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
  final AdminContractDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final c = detail.contract;
    final canInvalidate = c.status != 'Annule';

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Text(c.vehicleLabel,
            style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: AppColors.navy)),
        Text('${c.publicReference} · ${contractStatusLabel(c.status)}',
            style: const TextStyle(color: AppColors.steel)),
        const SizedBox(height: 12),
        AdminSection(
          title: 'Contrat',
          child: Column(
            children: [
              AdminRow('Prix convenu', fcfa(c.agreedPrice)),
              AdminRow('Date de vente',
                  DateFormat('d MMM yyyy', 'fr').format(c.saleDate.toLocal())),
              AdminRow('Vendeur', c.sellerName),
              AdminRow('Acheteur', c.buyerName),
              AdminRow('Véhicule',
                  '${c.vehicleLabel} ${detail.vehicleYear}'),
              if (detail.vehicleVin != null) AdminRow('VIN', detail.vehicleVin!),
              if (detail.registrationPlate != null)
                AdminRow('Immatriculation', detail.registrationPlate!),
              if (detail.verificationCode != null)
                AdminRow('Code de vérif.', detail.verificationCode!),
              AdminRow('Vente vérifiée', c.isVerifiedSale ? 'Oui' : 'Non'),
            ],
          ),
        ),
        AdminSection(
          title: 'Administration',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'L’administrateur ne peut qu’invalider un contrat, avec motif. Il ne valide jamais à la place des parties.',
                style: TextStyle(fontSize: 12, color: AppColors.steel),
              ),
              const SizedBox(height: 10),
              FilledButton.icon(
                onPressed: canInvalidate
                    ? () => _invalidate(context, ref, c.id)
                    : null,
                style: FilledButton.styleFrom(
                    backgroundColor:
                        canInvalidate ? AppColors.error : AppColors.silver),
                icon: const Icon(Icons.gpp_bad_outlined),
                label: Text(canInvalidate
                    ? 'Invalider le contrat'
                    : 'Contrat déjà annulé'),
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

  Future<void> _invalidate(
      BuildContext context, WidgetRef ref, String id) async {
    final reason = await askReason(
      context,
      title: 'Invalider ce contrat ?',
      hint: 'Motif de l’invalidation',
      confirmLabel: 'Invalider',
      destructive: true,
    );
    if (reason == null) return;
    try {
      await ref.read(adminRepositoryProvider).invalidateContract(id, reason);
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Action impossible.')),
        );
      }
      return;
    }
    ref.invalidate(adminContractProvider(id));
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Contrat invalidé.')),
      );
    }
  }
}
