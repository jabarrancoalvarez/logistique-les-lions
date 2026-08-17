import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../features/auth/providers/auth_providers.dart';
import '../../features/auth/ui/login_screen.dart';
import '../../features/auth/ui/register_screen.dart';
import '../../features/home/home_screen.dart';
import '../../features/splash/splash_screen.dart';

/// Rutas públicas que un usuario sin sesión puede ver.
const _publicRoutes = {'/login', '/register'};

/// Navegación de la app, dependiente del estado de sesión.
///
/// Redirige según el `AuthState`: mientras se restaura la sesión muestra el
/// splash; sin sesión fuerza a `/login`; con sesión iniciada aleja de las
/// pantallas de auth. Las pantallas de negocio se añaden fase a fase.
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

      final loggedIn = auth is Authenticated;
      final onPublic = _publicRoutes.contains(loc);

      if (!loggedIn) {
        // Sin sesión solo se permiten las pantallas públicas de auth.
        return onPublic ? null : '/login';
      }

      // Con sesión, aleja del splash y de las pantallas de auth.
      if (loc == '/splash' || onPublic) return '/';
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, _) => const RegisterScreen()),
      GoRoute(path: '/', builder: (_, _) => const HomeScreen()),
    ],
  );
});
