import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../../core/theme/app_colors.dart';
import '../../models/admin_common.dart';
import '../../models/admin_enums.dart';

/// Historial de `admin_actions` (append-only) de un objeto del backoffice.
class AdminActionsHistory extends StatelessWidget {
  const AdminActionsHistory({super.key, required this.actions, this.notes = const []});
  final List<AdminAction> actions;
  final List<AdminNote> notes;

  @override
  Widget build(BuildContext context) {
    if (actions.isEmpty && notes.isEmpty) {
      return const Text('Aucune action enregistrée.',
          style: TextStyle(color: AppColors.steel));
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final a in actions)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 6),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.history, size: 16, color: AppColors.steel),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(adminActionLabel(a.type),
                          style: const TextStyle(
                              fontWeight: FontWeight.w700, fontSize: 13)),
                      if (a.reason != null && a.reason!.isNotEmpty)
                        Text('« ${a.reason!} »',
                            style: const TextStyle(
                                fontSize: 12,
                                fontStyle: FontStyle.italic,
                                color: AppColors.steel)),
                      Text(
                        '${a.adminName} · ${DateFormat('d MMM yyyy, HH:mm', 'fr').format(a.createdAt.toLocal())}',
                        style: const TextStyle(
                            fontSize: 11, color: AppColors.steel),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        if (notes.isNotEmpty) ...[
          const SizedBox(height: 8),
          const Text('Notes internes',
              style: TextStyle(
                  fontWeight: FontWeight.w700, color: AppColors.navy)),
          for (final n in notes)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Text('• ${n.body} — ${n.adminName}',
                  style: const TextStyle(fontSize: 13)),
            ),
        ],
      ],
    );
  }
}

/// Tarjeta de sección con título (reutilizada por las fichas del backoffice).
class AdminSection extends StatelessWidget {
  const AdminSection({super.key, required this.title, required this.child});
  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.frostDark),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  color: AppColors.navy,
                  fontSize: 15)),
          const SizedBox(height: 8),
          child,
        ],
      ),
    );
  }
}

/// Fila etiqueta/valor.
class AdminRow extends StatelessWidget {
  const AdminRow(this.label, this.value, {super.key});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
              width: 130,
              child: Text(label,
                  style: const TextStyle(color: AppColors.steel, fontSize: 13))),
          Expanded(
            child: Text(value,
                style: const TextStyle(
                    fontWeight: FontWeight.w600,
                    color: AppColors.navyDark,
                    fontSize: 13)),
          ),
        ],
      ),
    );
  }
}
