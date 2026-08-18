import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/storage/secure_storage.dart';
import '../data/auth_repository.dart';
import '../models/app_user.dart';
import '../models/profile.dart';

/// Estado de la sesión de la app.
sealed class AuthState {
  const AuthState();
}

class AuthLoading extends AuthState {
  const AuthLoading();
}

class Authenticated extends AuthState {
  final AppUser user;
  const Authenticated(this.user);
}

class Unauthenticated extends AuthState {
  /// Mensaje de error del último intento, si lo hubo.
  final String? error;
  const Unauthenticated({this.error});
}

// ─── Providers base ──────────────────────────────────────────────────────────

final secureStorageProvider = Provider<SecureStorage>((ref) => SecureStorage());

final apiClientProvider = Provider<ApiClient>((ref) {
  // Sin conocer al controlador de sesión: el callback se asigna más abajo, ya
  // construido, para evitar un ciclo de dependencias.
  return ApiClient(ref.watch(secureStorageProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(apiClientProvider), ref.watch(secureStorageProvider));
});

/// Perfil completo del usuario autenticado, para la pantalla de edición.
final myProfileProvider = FutureProvider.autoDispose<Profile>(
  (ref) => ref.watch(authRepositoryProvider).getMyProfile(),
);

// ─── Controlador de sesión ───────────────────────────────────────────────────

final authControllerProvider =
    StateNotifierProvider<AuthController, AuthState>((ref) {
  final controller = AuthController(ref.watch(authRepositoryProvider));
  // Cuando el refresh falla, el cliente HTTP cierra la sesión en la app.
  ref.read(apiClientProvider).onSessionExpired = controller.forceLogout;
  controller.restore();
  return controller;
});

class AuthController extends StateNotifier<AuthState> {
  AuthController(this._repo) : super(const AuthLoading());

  final AuthRepository _repo;

  Future<void> restore() async {
    final user = await _repo.restore();
    state = user != null ? Authenticated(user) : const Unauthenticated();
  }

  Future<bool> login(String identifier, String password) async {
    state = const AuthLoading();
    try {
      final user = await _repo.login(identifier, password);
      state = Authenticated(user);
      return true;
    } catch (_) {
      state = const Unauthenticated(error: 'Identifiants incorrects.');
      return false;
    }
  }

  /// Alta de cuenta. Deja la sesión iniciada si tiene éxito.
  Future<String?> register({
    required String phone,
    required String password,
    required String displayName,
    required String accountType,
    String? region,
    String? city,
    String? email,
  }) async {
    state = const AuthLoading();
    try {
      final user = await _repo.register(
        phone: phone,
        password: password,
        displayName: displayName,
        accountType: accountType,
        region: region,
        city: city,
        email: email,
      );
      state = Authenticated(user);
      return null;
    } catch (e) {
      state = const Unauthenticated();
      return _messageFrom(e);
    }
  }

  /// Traduce el error de la API a un texto para el usuario.
  String _messageFrom(Object e) {
    final s = e.toString();
    if (s.contains('PhoneAlreadyExists') || s.contains('409')) {
      return 'Ce numéro est déjà enregistré.';
    }
    return 'Impossible de créer le compte. Réessayez.';
  }

  /// Tras editar el perfil: recarga el usuario guardado y refresca la sesión.
  Future<void> reloadProfile() async {
    try {
      final user = await _repo.reloadUser();
      if (user != null) state = Authenticated(user);
    } catch (_) {
      // Si falla la recarga, el perfil se actualizó igual en el servidor.
    }
  }

  Future<void> logout() async {
    await _repo.logout();
    state = const Unauthenticated();
  }

  /// La sesión caducó y no se pudo refrescar: limpieza silenciosa.
  void forceLogout() {
    _repo.logout();
    state = const Unauthenticated();
  }
}
