import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

/// Pantalla de arranque mientras se restaura la sesión guardada.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.heroBottom,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 48),
              child: Image.asset('assets/brand/logo.png',
                  width: 240, fit: BoxFit.contain),
            ),
            const SizedBox(height: 28),
            const SizedBox(
              height: 22,
              width: 22,
              child: CircularProgressIndicator(
                  strokeWidth: 2, color: AppColors.azureLight),
            ),
          ],
        ),
      ),
    );
  }
}
