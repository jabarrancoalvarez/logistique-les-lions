import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../models/admin_enums.dart';
import '../models/admin_report.dart';
import '../providers/admin_providers.dart';

class AdminReportsScreen extends StatelessWidget {
  const AdminReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 4,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Signalements'),
          bottom: const TabBar(
            isScrollable: true,
            tabs: [
              Tab(text: 'Nouveaux'),
              Tab(text: 'En examen'),
              Tab(text: 'Résolus'),
              Tab(text: 'Rejetés'),
            ],
          ),
        ),
        body: const TabBarView(
          children: [
            _ReportsTab(status: 'Nouveau'),
            _ReportsTab(status: 'EnExamen'),
            _ReportsTab(status: 'Resolu'),
            _ReportsTab(status: 'Rejete'),
          ],
        ),
      ),
    );
  }
}

class _ReportsTab extends ConsumerWidget {
  const _ReportsTab({required this.status});
  final String status;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(adminReportsProvider(status));

    return async.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (_, _) => Center(
        child: FilledButton(
          onPressed: () => ref.invalidate(adminReportsProvider(status)),
          child: const Text('Réessayer'),
        ),
      ),
      data: (list) {
        if (list.items.isEmpty) {
          return const Center(
              child: Text('Aucun signalement',
                  style: TextStyle(color: AppColors.steel)));
        }
        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(adminReportsProvider(status)),
          child: ListView.separated(
            itemCount: list.items.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (_, i) => _ReportTile(report: list.items[i]),
          ),
        );
      },
    );
  }
}

class _ReportTile extends StatelessWidget {
  const _ReportTile({required this.report});
  final ReportRow report;

  @override
  Widget build(BuildContext context) {
    final r = report;
    return ListTile(
      leading: CircleAvatar(
        backgroundColor: reportStatusColor(r.status).withValues(alpha: 0.12),
        child: Icon(Icons.flag_outlined,
            color: reportStatusColor(r.status), size: 20),
      ),
      title: Text(reportReasonLabel(r.reason),
          style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text(
        '${reportTargetLabel(r.targetType)} : ${r.targetLabel} · ${DateFormat('d MMM', 'fr').format(r.createdAt.toLocal())}',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
      trailing: Text(r.publicReference,
          style: const TextStyle(fontSize: 11, color: AppColors.steel)),
      onTap: () => context.push('/admin/reports/${r.id}'),
    );
  }
}
