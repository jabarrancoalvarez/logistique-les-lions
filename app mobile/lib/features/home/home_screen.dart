import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/theme/app_colors.dart';
import '../auth/providers/auth_providers.dart';

/// Pantalla provisional de la Fase 0.
///
/// No es todavía la portada real: sirve para comprobar que los cimientos funcionan —el
/// tema de marca, la conexión a la MISMA API de Render y el estado de sesión—. Las
/// pantallas de negocio llegan en las fases siguientes.
class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  int? _count;
  String _status = 'Connexion à l\'API…';

  @override
  void initState() {
    super.initState();
    _ping();
  }

  Future<void> _ping() async {
    try {
      final api = ref.read(apiClientProvider);
      final res = await api.dio.get('/vehicles/count');
      setState(() {
        _count = (res.data as Map<String, dynamic>)['count'] as int?;
        _status = 'API connectée';
      });
    } catch (_) {
      setState(() => _status = 'API indisponible (démarrage de Render ?)');
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Yoon u Auto')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.directions_car_filled, size: 64, color: AppColors.azureDark),
              const SizedBox(height: 16),
              const Text(
                'Yoon u Auto',
                style: TextStyle(
                    fontSize: 28, fontWeight: FontWeight.w800, color: AppColors.navy),
              ),
              const Text('Services Automobiles au Sénégal',
                  style: TextStyle(color: AppColors.steel)),
              const SizedBox(height: 32),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            _count != null ? Icons.check_circle : Icons.sync,
                            color: _count != null ? AppColors.success : AppColors.steel,
                            size: 18,
                          ),
                          const SizedBox(width: 8),
                          Text(_status),
                        ],
                      ),
                      if (_count != null) ...[
                        const SizedBox(height: 12),
                        Text('$_count véhicules disponibles',
                            style: const TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.w700,
                                color: AppColors.azureDark)),
                      ],
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                switch (auth) {
                  AuthLoading() => 'Session : vérification…',
                  Authenticated(:final user) => 'Connecté : ${user.displayName}',
                  Unauthenticated() => 'Non connecté',
                },
                style: const TextStyle(color: AppColors.steel),
              ),
              const SizedBox(height: 24),
              const Text(
                'Fondations (Phase 0) prêtes.\nLes écrans arrivent par phases.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.steel, fontSize: 12),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
