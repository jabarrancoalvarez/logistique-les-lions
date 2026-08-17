import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_providers.dart';

/// «Compte». Con sesión, muestra el perfil y permite cerrar sesión; sin ella,
/// ofrece iniciar sesión o crear una cuenta.
class AccountScreen extends ConsumerWidget {
  const AccountScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Compte')),
      body: auth is Authenticated
          ? _profile(context, ref, auth)
          : _guest(context),
    );
  }

  Widget _profile(BuildContext context, WidgetRef ref, Authenticated auth) {
    final user = auth.user;
    final accountLabel = user.isAdmin
        ? 'Administrateur'
        : (user.accountType ?? 'Particulier');

    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Row(
          children: [
            CircleAvatar(
              radius: 30,
              backgroundColor: AppColors.navy,
              child: Text(
                user.displayName.isNotEmpty
                    ? user.displayName[0].toUpperCase()
                    : '?',
                style: const TextStyle(
                    color: AppColors.white,
                    fontSize: 24,
                    fontWeight: FontWeight.w700),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(user.displayName,
                      style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                          color: AppColors.navy)),
                  const SizedBox(height: 2),
                  Text(accountLabel,
                      style: const TextStyle(color: AppColors.steel)),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 24),
        _infoTile(Icons.phone_outlined, 'Téléphone', user.phone ?? '—'),
        if (user.email != null && user.email!.isNotEmpty)
          _infoTile(Icons.email_outlined, 'E-mail', user.email!),
        const SizedBox(height: 24),
        OutlinedButton.icon(
          onPressed: () async {
            await ref.read(authControllerProvider.notifier).logout();
          },
          icon: const Icon(Icons.logout),
          label: const Text('Se déconnecter'),
          style: OutlinedButton.styleFrom(
            foregroundColor: AppColors.error,
            side: const BorderSide(color: AppColors.error),
            minimumSize: const Size.fromHeight(48),
          ),
        ),
        const SizedBox(height: 24),
        const Text(
          'D’autres sections (Mes annonces, Mon Garage, Négociations) arrivent dans les prochaines phases.',
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 12, color: AppColors.steel),
        ),
      ],
    );
  }

  Widget _infoTile(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, size: 20, color: AppColors.azureDark),
          const SizedBox(width: 14),
          Text('$label : ',
              style: const TextStyle(color: AppColors.steel)),
          Expanded(
            child: Text(value,
                style: const TextStyle(
                    fontWeight: FontWeight.w600, color: AppColors.navy)),
          ),
        ],
      ),
    );
  }

  Widget _guest(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.account_circle_outlined,
                size: 64, color: AppColors.silver),
            const SizedBox(height: 16),
            const Text('Bienvenue sur Yoon u Auto',
                style: TextStyle(
                    fontWeight: FontWeight.w800,
                    fontSize: 18,
                    color: AppColors.navy)),
            const SizedBox(height: 6),
            const Text(
              'Connectez-vous pour vendre, enregistrer des favoris et négocier.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.steel),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: () => context.push('/login'),
                child: const Text('Se connecter'),
              ),
            ),
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton(
                onPressed: () => context.push('/register'),
                child: const Text('Créer un compte'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
