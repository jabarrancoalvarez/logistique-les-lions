import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../providers/auth_providers.dart';
import 'auth_scaffold.dart';

/// Inicio de sesión. El identificador es el teléfono (+221XXXXXXXXX) o el correo.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _identifier = TextEditingController();
  final _password = TextEditingController();
  bool _obscure = true;
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _identifier.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _submitting = true;
      _error = null;
    });
    final ok = await ref
        .read(authControllerProvider.notifier)
        .login(_identifier.text.trim(), _password.text);
    if (!mounted) return;
    if (ok) {
      context.go('/');
    } else {
      setState(() {
        _submitting = false;
        _error = 'Identifiants incorrects.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Bon retour',
      subtitle: 'Connectez-vous à votre compte',
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
              controller: _identifier,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(
                labelText: 'Téléphone',
                hintText: '+221 77 123 45 67',
              ),
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Champ requis' : null,
            ),
            const SizedBox(height: 6),
            const Text('Vous pouvez aussi utiliser votre adresse e-mail.',
                style: TextStyle(fontSize: 12, color: AppColors.steel)),
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
              validator: (v) =>
                  (v == null || v.isEmpty) ? 'Champ requis' : null,
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
                  : const Text('Se connecter'),
            ),
            const SizedBox(height: 16),
            Center(
              child: TextButton(
                onPressed: () => context.go('/register'),
                child: const Text.rich(TextSpan(children: [
                  TextSpan(
                      text: 'Pas encore de compte ? ',
                      style: TextStyle(color: AppColors.steel)),
                  TextSpan(
                      text: 'Créer un compte',
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
