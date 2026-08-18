import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

/// Pantalla de arranque mientras se restaura la sesión guardada. Fondo celeste de
/// marca con un halo suave, el logotipo centrado y un indicador discreto.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [AppColors.heroTop, AppColors.heroBottom, AppColors.navyDark],
            stops: [0.0, 0.55, 1.0],
          ),
        ),
        child: Stack(
          children: [
            // Halo luminoso detrás del logo.
            Align(
              alignment: const Alignment(0, -0.15),
              child: Container(
                width: 320,
                height: 320,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: RadialGradient(
                    colors: [
                      Colors.white.withValues(alpha: 0.16),
                      Colors.white.withValues(alpha: 0.0),
                    ],
                  ),
                ),
              ),
            ),
            Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 44),
                    child: Image.asset('assets/brand/logo.png',
                        width: 260, fit: BoxFit.contain),
                  ),
                  const SizedBox(height: 10),
                  Text(
                    'Services Automobiles au Sénégal',
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.9),
                      fontSize: 13,
                      letterSpacing: 0.3,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
            // Indicador de carga discreto abajo.
            Align(
              alignment: const Alignment(0, 0.82),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SizedBox(
                    height: 26,
                    width: 26,
                    child: CircularProgressIndicator(
                      strokeWidth: 2.4,
                      valueColor: AlwaysStoppedAnimation(
                          Colors.white.withValues(alpha: 0.9)),
                    ),
                  ),
                  const SizedBox(height: 14),
                  Text(
                    'Chargement…',
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.7),
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
