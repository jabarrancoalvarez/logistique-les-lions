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
      body: ListView(
        padding: EdgeInsets.zero,
        children: [
          _Hero(name: name, showBell: auth is Authenticated),
          Transform.translate(
            offset: const Offset(0, -26),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: _CtaButton(),
            ),
          ),
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 4, 20, 8),
            child: Text(
              'Pourquoi Yoon u Auto ?',
              style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w800,
                  color: AppColors.navy),
            ),
          ),
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 0, 20, 28),
            child: Column(
              children: [
                _ValueCard(
                  icon: Icons.verified_user_outlined,
                  color: AppColors.success,
                  title: 'Ventes vérifiées',
                  text:
                      'Contrats numériques et code de vérification pour chaque transaction.',
                ),
                SizedBox(height: 12),
                _ValueCard(
                  icon: Icons.forum_outlined,
                  color: AppColors.azureDark,
                  title: 'Négociation intégrée',
                  text:
                      'Discutez, faites une offre et organisez l’inspection au même endroit.',
                ),
                SizedBox(height: 12),
                _ValueCard(
                  icon: Icons.garage_outlined,
                  color: AppColors.warning,
                  title: 'Mon Garage',
                  text:
                      'Suivez l’entretien et l’historique de vos véhicules en privé.',
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Hero extends StatelessWidget {
  const _Hero({this.name, required this.showBell});
  final String? name;
  final bool showBell;

  @override
  Widget build(BuildContext context) {
    final topInset = MediaQuery.of(context).padding.top;
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.heroTop, AppColors.heroBottom, AppColors.navyDark],
          stops: [0.0, 0.6, 1.0],
        ),
        borderRadius: BorderRadius.only(
          bottomLeft: Radius.circular(30),
          bottomRight: Radius.circular(30),
        ),
      ),
      child: Stack(
        children: [
          // Halo decorativo.
          Positioned(
            top: -40,
            right: -30,
            child: Container(
              width: 200,
              height: 200,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: RadialGradient(colors: [
                  Colors.white.withValues(alpha: 0.14),
                  Colors.white.withValues(alpha: 0.0),
                ]),
              ),
            ),
          ),
          Padding(
            padding: EdgeInsets.fromLTRB(20, topInset + 8, 12, 34),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Campana arriba a la derecha (solo con sesión).
                SizedBox(
                  height: 44,
                  child: showBell
                      ? Align(
                          alignment: Alignment.centerRight,
                          child: IconTheme.merge(
                            data: const IconThemeData(color: Colors.white),
                            child: const NotificationBell(),
                          ),
                        )
                      : null,
                ),
                const SizedBox(height: 6),
                Image.asset('assets/brand/logo.png',
                    height: 64, fit: BoxFit.contain),
                const SizedBox(height: 18),
                Text(
                  name != null
                      ? 'Bonjour, $name 👋'
                      : 'Services Automobiles au Sénégal',
                  style: const TextStyle(
                      color: AppColors.white,
                      fontSize: 22,
                      height: 1.2,
                      fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 6),
                Text(
                  'Achetez et vendez en toute confiance.',
                  style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.85),
                      fontSize: 14),
                ),
                const SizedBox(height: 16),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CtaButton extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Material(
      elevation: 6,
      shadowColor: AppColors.azureDark.withValues(alpha: 0.4),
      borderRadius: BorderRadius.circular(14),
      child: FilledButton.icon(
        onPressed: () => context.go('/vehicules'),
        icon: const Icon(Icons.directions_car_filled),
        label: const Text('Parcourir les véhicules'),
        style: FilledButton.styleFrom(
          minimumSize: const Size.fromHeight(56),
          backgroundColor: AppColors.azureDark,
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        ),
      ),
    );
  }
}

class _ValueCard extends StatelessWidget {
  const _ValueCard({
    required this.icon,
    required this.color,
    required this.title,
    required this.text,
  });
  final IconData icon;
  final Color color;
  final String title;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.frostDark),
        boxShadow: [
          BoxShadow(
            color: AppColors.navy.withValues(alpha: 0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(11),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: color, size: 24),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title,
                    style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        fontSize: 15.5,
                        color: AppColors.navy)),
                const SizedBox(height: 3),
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
