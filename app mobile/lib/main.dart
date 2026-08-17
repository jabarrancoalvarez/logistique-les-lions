import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  // Datos de fecha en francés para los formatos relativos del chat.
  initializeDateFormatting('fr');
  runApp(const ProviderScope(child: YoonUAutoApp()));
}

class YoonUAutoApp extends ConsumerWidget {
  const YoonUAutoApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      title: 'Yoon u Auto',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      routerConfig: ref.watch(routerProvider),
    );
  }
}
