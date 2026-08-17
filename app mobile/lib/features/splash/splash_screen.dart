import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

/// Pantalla de arranque mientras se restaura la sesión guardada.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: AppColors.navy,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.directions_car_filled, size: 64, color: AppColors.white),
            SizedBox(height: 20),
            Text(
              'Yoon u Auto',
              style: TextStyle(
                  color: AppColors.white,
                  fontSize: 24,
                  fontWeight: FontWeight.w800),
            ),
            SizedBox(height: 24),
            SizedBox(
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
