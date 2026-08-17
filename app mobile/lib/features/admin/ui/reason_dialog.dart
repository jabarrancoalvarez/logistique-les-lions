import 'package:flutter/material.dart';

/// Pide un **motivo** obligatorio. Toda medida del backoffice que afecte a lo que
/// el usuario ve exige uno, y el backend lo registra en `admin_actions`.
Future<String?> askReason(
  BuildContext context, {
  required String title,
  String hint = 'Motif (obligatoire)',
  String confirmLabel = 'Confirmer',
  bool destructive = false,
}) {
  final controller = TextEditingController();
  return showDialog<String>(
    context: context,
    builder: (ctx) {
      String? error;
      return StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(title),
          content: TextField(
            controller: controller,
            maxLines: 3,
            autofocus: true,
            decoration: InputDecoration(hintText: hint, errorText: error),
            onChanged: (_) {
              if (error != null) setState(() => error = null);
            },
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Annuler')),
            FilledButton(
              style: destructive
                  ? FilledButton.styleFrom(backgroundColor: Colors.red.shade700)
                  : null,
              onPressed: () {
                final text = controller.text.trim();
                if (text.isEmpty) {
                  setState(() => error = 'Le motif est obligatoire.');
                  return;
                }
                Navigator.pop(ctx, text);
              },
              child: Text(confirmLabel),
            ),
          ],
        ),
      );
    },
  );
}
