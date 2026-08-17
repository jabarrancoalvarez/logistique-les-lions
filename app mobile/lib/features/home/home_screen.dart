import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/theme/app_colors.dart';
import '../auth/providers/auth_providers.dart';

/// Portada tras iniciar sesión.
///
/// Provisional: confirma la sesión activa contra la MISMA API de Render y ofrece
/// cerrar sesión. Las pantallas de negocio (marketplace, ficha, negociación,
/// garaje…) llegan en las fases siguientes.
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
      if (!mounted) return;
      setState(() {
        _count = (res.data as Map<String, dynamic>)['count'] as int?;
        _status = 'API connectée';
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _status = 'API indisponible (démarrage de Render ?)');
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authControllerProvider);
    final user = auth is Authenticated ? auth.user : null;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Yoon u Auto'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Se déconnecter',
            onPressed: () =>
                ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.directions_car_filled,
                  size: 64, color: AppColors.azureDark),
              const SizedBox(height: 16),
              Text(
                user != null ? 'Bonjour, ${user.displayName}' : 'Yoon u Auto',
                style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                    color: AppColors.navy),
                textAlign: TextAlign.center,
              ),
              if (user != null)
                Text(
                  '${user.phone ?? ''} · ${user.isAdmin ? 'Admin' : user.accountType ?? 'Particulier'}',
                  style: const TextStyle(color: AppColors.steel),
                ),
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
                            color:
                                _count != null ? AppColors.success : AppColors.steel,
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
              const SizedBox(height: 24),
              const Text(
                'Authentification (Phase 1) prête.\nLes écrans de vente arrivent en Phase 2.',
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
