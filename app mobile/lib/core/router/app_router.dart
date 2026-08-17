import 'package:go_router/go_router.dart';
import '../../features/home/home_screen.dart';

/// Navegación de la app. En la Fase 0 solo hay una ruta; las demás se irán
/// añadiendo (login, marketplace, ficha, negociación, garaje…) fase a fase.
final appRouter = GoRouter(
  initialLocation: '/',
  routes: [
    GoRoute(
      path: '/',
      builder: (context, state) => const HomeScreen(),
    ),
  ],
);
