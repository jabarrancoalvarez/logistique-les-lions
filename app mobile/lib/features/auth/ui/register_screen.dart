import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/data/senegal_regions.dart';
import '../../../core/theme/app_colors.dart';
import '../providers/auth_providers.dart';
import 'auth_scaffold.dart';

/// Alta de cuenta. El teléfono es el identificador; el correo es opcional.
class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _displayName = TextEditingController();
  final _phone = TextEditingController(text: '+221');
  final _city = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();

  String _accountType = 'Particulier';
  String? _region;
  bool _obscure = true;
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _displayName.dispose();
    _phone.dispose();
    _city.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  String? _validatePhone(String? v) {
    final value = (v ?? '').replaceAll(' ', '');
    if (value.isEmpty) return 'Champ requis';
    if (!RegExp(r'^\+221\d{9}$').hasMatch(value)) {
      return 'Format : +221 suivi de 9 chiffres';
    }
    return null;
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _submitting = true;
      _error = null;
    });
    final error = await ref.read(authControllerProvider.notifier).register(
          phone: _phone.text.replaceAll(' ', '').trim(),
          password: _password.text,
          displayName: _displayName.text.trim(),
          accountType: _accountType,
          region: _region,
          city: _city.text.trim().isEmpty ? null : _city.text.trim(),
          email: _email.text.trim(),
        );
    if (!mounted) return;
    if (error == null) {
      context.go('/');
    } else {
      setState(() {
        _submitting = false;
        _error = error;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Créer un compte',
      subtitle: 'Gratuit et sans engagement',
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_error != null) ...[
              _ErrorBanner(_error!),
              const SizedBox(height: 12),
            ],
            TextFormField(
              controller: _displayName,
              textCapitalization: TextCapitalization.words,
              decoration: const InputDecoration(labelText: 'Nom complet'),
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Champ requis' : null,
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _phone,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(
                labelText: 'Téléphone',
                hintText: '+221 77 123 45 67',
              ),
              validator: _validatePhone,
            ),
            const SizedBox(height: 16),
            _AccountTypeSelector(
              value: _accountType,
              onChanged: (v) => setState(() => _accountType = v),
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<String>(
              initialValue: _region,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Région'),
              items: [
                for (final r in senegalRegions)
                  DropdownMenuItem(value: r.code, child: Text(r.name)),
              ],
              onChanged: (v) => setState(() => _region = v),
              validator: (v) => v == null ? 'Choisissez une région' : null,
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _city,
              textCapitalization: TextCapitalization.words,
              decoration: const InputDecoration(
                labelText: 'Ville (facultatif)',
              ),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(
                labelText: 'E-mail (facultatif)',
                helperText: 'Uniquement pour les notifications',
              ),
              validator: (v) {
                final value = (v ?? '').trim();
                if (value.isEmpty) return null;
                return RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value)
                    ? null
                    : 'E-mail invalide';
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _password,
              obscureText: _obscure,
              decoration: InputDecoration(
                labelText: 'Mot de passe',
                suffixIcon: IconButton(
                  icon: Icon(_obscure ? Icons.visibility : Icons.visibility_off),
                  onPressed: () => setState(() => _obscure = !_obscure),
                ),
              ),
              validator: (v) => (v == null || v.length < 8)
                  ? '8 caractères minimum'
                  : null,
              onFieldSubmitted: (_) => _submit(),
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(
                      height: 20, width: 20,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: Colors.white))
                  : const Text('Créer mon compte'),
            ),
            const SizedBox(height: 16),
            Center(
              child: TextButton(
                onPressed: () => context.go('/login'),
                child: const Text.rich(TextSpan(children: [
                  TextSpan(
                      text: 'Déjà un compte ? ',
                      style: TextStyle(color: AppColors.steel)),
                  TextSpan(
                      text: 'Se connecter',
                      style: TextStyle(
                          color: AppColors.azureDark,
                          fontWeight: FontWeight.w600)),
                ])),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AccountTypeSelector extends StatelessWidget {
  const _AccountTypeSelector({required this.value, required this.onChanged});
  final String value;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Padding(
          padding: EdgeInsets.only(left: 4, bottom: 6),
          child: Text('Type de compte',
              style: TextStyle(fontSize: 12, color: AppColors.steel)),
        ),
        Row(
          children: [
            Expanded(
              child: _TypeChip(
                label: 'Particulier',
                selected: value == 'Particulier',
                onTap: () => onChanged('Particulier'),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _TypeChip(
                label: 'Professionnel',
                selected: value == 'Professionnel',
                onTap: () => onChanged('Professionnel'),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _TypeChip extends StatelessWidget {
  const _TypeChip(
      {required this.label, required this.selected, required this.onTap});
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 14),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: selected ? AppColors.azure.withValues(alpha: 0.12) : AppColors.frost,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: selected ? AppColors.azureDark : AppColors.silver,
            width: selected ? 1.6 : 1,
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: selected ? AppColors.azureDark : AppColors.steel,
          ),
        ),
      ),
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner(this.message);
  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.3)),
      ),
      child: Row(children: [
        const Icon(Icons.error_outline, color: AppColors.error, size: 18),
        const SizedBox(width: 8),
        Expanded(
            child: Text(message,
                style: const TextStyle(color: AppColors.error, fontSize: 13))),
      ]),
    );
  }
}
