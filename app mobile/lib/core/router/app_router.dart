import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../features/account/ui/account_screen.dart';
import '../../features/auth/providers/auth_providers.dart';
import '../../features/auth/ui/login_screen.dart';
import '../../features/auth/ui/register_screen.dart';
import '../../features/favorites/ui/favorites_screen.dart';
import '../../features/home/home_screen.dart';
import '../../features/negotiations/ui/negotiation_chat_screen.dart';
import '../../features/negotiations/ui/negotiations_list_screen.dart';
import '../../features/shell/main_shell.dart';
import '../../features/splash/splash_screen.dart';
import '../../features/vehicles/ui/marketplace_screen.dart';
import '../../features/vehicles/ui/vehicle_detail_screen.dart';

/// Navegación de la app.
///
/// El escaparate se puede recorrer **sin sesión** (igual que la web): solo el
/// splash (mientras se restaura la sesión) y las pantallas de auth quedan fuera
/// del shell. Iniciar sesión se pide de forma contextual (favoritos, contacto…).
final routerProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(authControllerProvider, (_, _) => refresh.value++);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: '/',
    refreshListenable: refresh,
    redirect: (context, state) {
      final auth = ref.read(authControllerProvider);
      final loc = state.matchedLocation;

      if (auth is AuthLoading) {
        return loc == '/splash' ? null : '/splash';
      }
      // Sesión resuelta: nadie se queda en el splash.
      if (loc == '/splash') return '/';
      // Con sesión, no tiene sentido ver login/registro.
      if (auth is Authenticated && (loc == '/login' || loc == '/register')) {
        return '/';
      }
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, _) => const RegisterScreen()),
      GoRoute(
        path: '/vehicules/:slug',
        builder: (_, state) =>
            VehicleDetailScreen(slug: state.pathParameters['slug']!),
      ),
      GoRoute(
        path: '/negociations/:id',
        builder: (_, state) =>
            NegotiationChatScreen(negotiationId: state.pathParameters['id']!),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            MainShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/', builder: (_, _) => const HomeScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
                path: '/vehicules',
                builder: (_, _) => const MarketplaceScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/favoris', builder: (_, _) => const FavoritesScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
                path: '/negociations',
                builder: (_, _) => const NegotiationsListScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/compte', builder: (_, _) => const AccountScreen()),
          ]),
        ],
      ),
    ],
  );
});
