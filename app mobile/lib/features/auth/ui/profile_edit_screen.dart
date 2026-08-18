import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/data/senegal_regions.dart';
import '../../../core/theme/app_colors.dart';
import '../models/profile.dart';
import '../providers/auth_providers.dart';

/// Edición del perfil del usuario. El teléfono es la identidad verificada y no
/// se puede cambiar; el resto (nombre, tipo de cuenta, región, ciudad, e-mail,
/// bio, contacto WhatsApp) sí.
class ProfileEditScreen extends ConsumerWidget {
  const ProfileEditScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(myProfileProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Modifier le profil')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => Center(
          child: FilledButton(
            onPressed: () => ref.invalidate(myProfileProvider),
            child: const Text('Réessayer'),
          ),
        ),
        data: (profile) => _Form(profile: profile),
      ),
    );
  }
}

class _Form extends ConsumerStatefulWidget {
  const _Form({required this.profile});
  final Profile profile;

  @override
  ConsumerState<_Form> createState() => _FormState();
}

class _FormState extends ConsumerState<_Form> {
  final _formKey = GlobalKey<FormState>();
  late String _accountType = widget.profile.accountType;
  late String? _region = widget.profile.region;
  late bool _whatsapp = widget.profile.allowWhatsAppContact;

  late final _name = TextEditingController(text: widget.profile.displayName);
  late final _city = TextEditingController(text: widget.profile.city ?? '');
  late final _email = TextEditingController(text: widget.profile.email ?? '');
  late final _bio = TextEditingController(text: widget.profile.bio ?? '');
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _city.dispose();
    _email.dispose();
    _bio.dispose();
    super.dispose();
  }

  String? _opt(TextEditingController c) =>
      c.text.trim().isEmpty ? null : c.text.trim();

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      await ref.read(authRepositoryProvider).updateProfile(
            displayName: _name.text.trim(),
            accountType: _accountType,
            region: _region,
            city: _opt(_city),
            email: _opt(_email),
            bio: _opt(_bio),
            allowWhatsAppContact: _whatsapp,
          );
      // Refresca el usuario guardado para que Compte muestre los cambios.
      await ref.read(authControllerProvider.notifier).reloadProfile();
      ref.invalidate(myProfileProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Profil mis à jour.')),
      );
      context.pop();
    } catch (_) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mise à jour impossible. Réessayez.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final p = widget.profile;
    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
        children: [
          // Teléfono: identidad verificada, no editable.
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.frost,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: AppColors.frostDark),
            ),
            child: Row(
              children: [
                const Icon(Icons.phone_outlined,
                    size: 18, color: AppColors.steel),
                const SizedBox(width: 10),
                Expanded(
                  child: Text('${p.phone ?? '—'}${p.phoneVerified ? '  ✓' : ''}',
                      style: const TextStyle(
                          fontWeight: FontWeight.w600, color: AppColors.navy)),
                ),
                const Text('Non modifiable',
                    style: TextStyle(fontSize: 11, color: AppColors.steel)),
              ],
            ),
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _name,
            textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(labelText: 'Nom complet'),
            validator: (v) =>
                (v == null || v.trim().isEmpty) ? 'Champ requis' : null,
          ),
          const SizedBox(height: 16),
          _AccountTypeSelector(
            value: _accountType,
            onChanged: (v) => setState(() => _accountType = v),
          ),
          const SizedBox(height: 16),
          DropdownButtonFormField<String>(
            initialValue: senegalRegions.any((r) => r.code == _region)
                ? _region
                : null,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Région'),
            items: [
              const DropdownMenuItem(value: null, child: Text('—')),
              for (final r in senegalRegions)
                DropdownMenuItem(value: r.code, child: Text(r.name)),
            ],
            onChanged: (v) => setState(() => _region = v),
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _city,
            textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(labelText: 'Ville (facultatif)'),
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
            controller: _bio,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: 'Bio (facultatif)',
              alignLabelWithHint: true,
            ),
          ),
          const SizedBox(height: 8),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            value: _whatsapp,
            onChanged: (v) => setState(() => _whatsapp = v),
            title: const Text('Autoriser le contact WhatsApp'),
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _saving ? null : _save,
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
            child: _saving
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                        strokeWidth: 2, color: Colors.white))
                : const Text('Enregistrer'),
          ),
        ],
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
    Widget chip(String key, String label) {
      final selected = value == key;
      return Expanded(
        child: InkWell(
          onTap: () => onChanged(key),
          borderRadius: BorderRadius.circular(12),
          child: Container(
            padding: const EdgeInsets.symmetric(vertical: 14),
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: selected
                  ? AppColors.azure.withValues(alpha: 0.12)
                  : AppColors.frost,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: selected ? AppColors.azureDark : AppColors.silver,
                width: selected ? 1.6 : 1,
              ),
            ),
            child: Text(label,
                style: TextStyle(
                    fontWeight: FontWeight.w600,
                    color: selected ? AppColors.azureDark : AppColors.steel)),
          ),
        ),
      );
    }

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
            chip('Particulier', 'Particulier'),
            const SizedBox(width: 12),
            chip('Professionnel', 'Professionnel'),
          ],
        ),
      ],
    );
  }
}
