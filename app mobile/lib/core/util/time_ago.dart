import 'package:intl/intl.dart';

/// Tiempo relativo corto en francés: «à l’instant», «5 min», «3 h», «hier»,
/// o la fecha si es antiguo.
String timeAgo(DateTime? when) {
  if (when == null) return '';
  final now = DateTime.now();
  final diff = now.difference(when.toLocal());

  if (diff.inMinutes < 1) return 'à l’instant';
  if (diff.inMinutes < 60) return '${diff.inMinutes} min';
  if (diff.inHours < 24) return '${diff.inHours} h';
  if (diff.inDays == 1) return 'hier';
  if (diff.inDays < 7) return '${diff.inDays} j';
  return DateFormat('d MMM', 'fr').format(when.toLocal());
}

/// Hora del mensaje: «14:32».
String messageTime(DateTime when) =>
    DateFormat('HH:mm', 'fr').format(when.toLocal());
