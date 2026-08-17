import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';

/// Resultado del formulario de oferta.
class OfferInput {
  final num amount;
  final String? message;
  const OfferInput(this.amount, this.message);
}

/// Modal «Faire une offre» / «Contre-offre». Devuelve [OfferInput] o `null`.
Future<OfferInput?> showOfferSheet(
  BuildContext context, {
  required num listedPrice,
  String title = 'Faire une offre',
}) {
  return showModalBottomSheet<OfferInput>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (_) => _OfferSheet(listedPrice: listedPrice, title: title),
  );
}

class _OfferSheet extends StatefulWidget {
  const _OfferSheet({required this.listedPrice, required this.title});
  final num listedPrice;
  final String title;

  @override
  State<_OfferSheet> createState() => _OfferSheetState();
}

class _OfferSheetState extends State<_OfferSheet> {
  final _amount = TextEditingController();
  final _message = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _amount.dispose();
    _message.dispose();
    super.dispose();
  }

  void _submit() {
    final value = num.tryParse(_amount.text.trim());
    if (value == null || value <= 0) {
      setState(() => _error = 'Saisissez un montant valide.');
      return;
    }
    Navigator.pop(
      context,
      OfferInput(value, _message.text.trim().isEmpty ? null : _message.text.trim()),
    );
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;
    return Padding(
      padding: EdgeInsets.only(bottom: bottomInset),
      child: Container(
        decoration: const BoxDecoration(
          color: AppColors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: AppColors.silver,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(widget.title,
                style: const TextStyle(
                    fontSize: 18, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            Text('Prix affiché : ${fcfa(widget.listedPrice)}',
                style: const TextStyle(color: AppColors.steel, fontSize: 13)),
            const SizedBox(height: 16),
            TextField(
              controller: _amount,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              autofocus: true,
              decoration: InputDecoration(
                labelText: 'Votre offre (FCFA)',
                errorText: _error,
                prefixIcon: const Icon(Icons.payments_outlined),
              ),
              onChanged: (_) {
                if (_error != null) setState(() => _error = null);
              },
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _message,
              maxLines: 2,
              decoration: const InputDecoration(
                labelText: 'Message (facultatif)',
                alignLabelWithHint: true,
              ),
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _submit,
              style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
              child: const Text('Envoyer l’offre'),
            ),
          ],
        ),
      ),
    );
  }
}
