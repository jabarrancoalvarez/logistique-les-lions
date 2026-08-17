import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../models/admin_enums.dart';
import '../models/admin_report.dart';
import '../providers/admin_providers.dart';
import 'reason_dialog.dart';
import 'widgets/admin_history.dart';

class AdminReportDetailScreen extends ConsumerWidget {
  const AdminReportDetailScreen({super.key, required this.id});
  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminReportProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Signalement')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(adminReportProvider(id)),
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
  final ReportDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final r = detail.report;

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Row(
          children: [
            Expanded(
              child: Text(reportReasonLabel(r.reason),
                  style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: AppColors.navy)),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: reportStatusColor(r.status).withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(reportStatusLabel(r.status),
                  style: TextStyle(
                      fontWeight: FontWeight.w700,
                      color: reportStatusColor(r.status))),
            ),
          ],
        ),
        const SizedBox(height: 4),
        Text(r.publicReference,
            style: const TextStyle(color: AppColors.steel)),
        const SizedBox(height: 12),
        AdminSection(
          title: 'Détails',
          child: Column(
            children: [
              AdminRow('Cible', '${reportTargetLabel(r.targetType)} : ${r.targetLabel}'),
              AdminRow('Signalé par', r.reporterName),
              if (r.reportedUserName != null)
                AdminRow('Utilisateur visé', r.reportedUserName!),
              if (r.description != null && r.description!.isNotEmpty)
                AdminRow('Description', r.description!),
              if (detail.otherOpenReports > 0)
                AdminRow('Autres ouverts', '${detail.otherOpenReports}'),
              if (detail.handledByAdminName != null)
                AdminRow('Traité par', detail.handledByAdminName!),
              if (detail.resolution != null)
                AdminRow('Résolution', detail.resolution!),
            ],
          ),
        ),
        if (r.targetType == 'Listing')
          OutlinedButton.icon(
            onPressed: () => context.push('/admin/listings/${r.targetId}'),
            icon: const Icon(Icons.directions_car_outlined, size: 18),
            label: const Text('Ouvrir l’annonce'),
          ),
        if (r.targetType == 'User')
          OutlinedButton.icon(
            onPressed: () => context.push('/admin/users/${r.targetId}'),
            icon: const Icon(Icons.person_outline, size: 18),
            label: const Text('Ouvrir l’utilisateur'),
          ),
        const SizedBox(height: 12),
        AdminSection(
          title: 'Traitement',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final s in reportStatusValues)
                    if (s != r.status)
                      OutlinedButton(
                        onPressed: () => _setStatus(context, ref, r.id, s),
                        child: Text(reportStatusLabel(s)),
                      ),
                ],
              ),
              const SizedBox(height: 8),
              if (r.reportedUserId != null)
                FilledButton.icon(
                  onPressed: () => _warn(context, ref, r.id),
                  style: FilledButton.styleFrom(
                      backgroundColor: AppColors.warning),
                  icon: const Icon(Icons.warning_amber_outlined),
                  label: const Text('Avertir l’utilisateur'),
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

  Future<void> _setStatus(
      BuildContext context, WidgetRef ref, String id, String status) async {
    final resolution = await askReason(
      context,
      title: 'Passer en « ${reportStatusLabel(status)} »',
      hint: 'Résolution / motif',
      confirmLabel: 'Confirmer',
    );
    if (resolution == null) return;
    try {
      await ref
          .read(adminRepositoryProvider)
          .setReportStatus(id, status: status, resolution: resolution);
    } catch (_) {
      if (context.mounted) _snack(context, 'Action impossible.');
      return;
    }
    ref.invalidate(adminReportProvider(id));
    if (context.mounted) _snack(context, 'Signalement mis à jour.');
  }

  Future<void> _warn(BuildContext context, WidgetRef ref, String id) async {
    final message = await askReason(
      context,
      title: 'Avertir l’utilisateur',
      hint: 'Message d’avertissement',
      confirmLabel: 'Envoyer',
      destructive: true,
    );
    if (message == null) return;
    try {
      await ref.read(adminRepositoryProvider).warnReportedUser(id, message);
    } catch (_) {
      if (context.mounted) _snack(context, 'Action impossible.');
      return;
    }
    ref.invalidate(adminReportProvider(id));
    if (context.mounted) _snack(context, 'Avertissement envoyé.');
  }

  void _snack(BuildContext context, String m) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));
  }
}
