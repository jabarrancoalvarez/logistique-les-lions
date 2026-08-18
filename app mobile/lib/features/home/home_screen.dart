import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../auth/providers/auth_providers.dart';
import '../notifications/ui/notification_bell.dart';

/// «Accueil» — portada de la app. Presenta la marca y lleva al escaparate.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authControllerProvider);
    final name = auth is Authenticated ? auth.user.displayName : null;

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            expandedHeight: 220,
            automaticallyImplyLeading: false,
            actions: const [NotificationBell()],
            flexibleSpace: FlexibleSpaceBar(
              background: _Hero(name: name),
            ),
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 24, 20, 32),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  FilledButton.icon(
                    onPressed: () => context.go('/vehicules'),
                    icon: const Icon(Icons.directions_car_filled),
                    label: const Text('Parcourir les véhicules'),
                    style: FilledButton.styleFrom(
                      minimumSize: const Size.fromHeight(52),
                    ),
                  ),
                  const SizedBox(height: 24),
                  const _ValueProp(
                    icon: Icons.verified_user_outlined,
                    title: 'Ventes vérifiées',
                    text:
                        'Contrats numériques et code de vérification pour chaque transaction.',
                  ),
                  const _ValueProp(
                    icon: Icons.forum_outlined,
                    title: 'Négociation intégrée',
                    text:
                        'Discutez, faites une offre et organisez l’inspection au même endroit.',
                  ),
                  const _ValueProp(
                    icon: Icons.garage_outlined,
                    title: 'Mon Garage',
                    text:
                        'Suivez l’entretien et l’historique de vos véhicules en privé.',
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Hero extends StatelessWidget {
  const _Hero({this.name});
  final String? name;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [AppColors.heroTop, AppColors.heroBottom],
        ),
      ),
      padding: const EdgeInsets.fromLTRB(20, 60, 20, 24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.end,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Image.asset('assets/brand/logo.png', height: 66, fit: BoxFit.contain),
          const SizedBox(height: 14),
          Text(
            name != null ? 'Bonjour, $name' : 'Services Automobiles au Sénégal',
            style: const TextStyle(
                color: AppColors.white,
                fontSize: 22,
                fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 4),
          Text(
            'Achetez et vendez en toute confiance.',
            style: TextStyle(color: AppColors.white.withValues(alpha: 0.85)),
          ),
        ],
      ),
    );
  }
}

class _ValueProp extends StatelessWidget {
  const _ValueProp(
      {required this.icon, required this.title, required this.text});
  final IconData icon;
  final String title;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 18),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: AppColors.frostDark,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: AppColors.azureDark, size: 22),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title,
                    style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 15,
                        color: AppColors.navy)),
                const SizedBox(height: 2),
                Text(text,
                    style: const TextStyle(
                        color: AppColors.steel, height: 1.4, fontSize: 13)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
