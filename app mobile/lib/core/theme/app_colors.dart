import 'package:flutter/material.dart';

/// Paleta de marca de Yoon u Auto, tomada de la web (medida del logo).
///
/// Solo azules y blanco. El azul más profundo del logo es el «navy» océano; el celeste
/// vivo es el acento. Cualquier pantalla nueva usa estos tokens, nunca colores sueltos.
class AppColors {
  AppColors._();

  // Azul océano (base de marca)
  static const navy = Color(0xFF14567F);
  static const navyLight = Color(0xFF1E7BA8);
  static const navyDark = Color(0xFF0E3E5C);

  // Celeste (acento)
  static const azure = Color(0xFF26AEE0);
  static const azureLight = Color(0xFF7FD3EC);
  static const azureDark = Color(0xFF157FA8);

  // Degradado del hero / splash: azul celeste del logo, con un fondo algo más
  // profundo abajo para que el texto blanco se lea bien.
  static const heroTop = Color(0xFF23A2DA);
  static const heroBottom = Color(0xFF12699F);

  // Blancos y plata
  static const frost = Color(0xFFF5FAFD);
  static const frostDark = Color(0xFFE6EFF6);
  static const silver = Color(0xFFC7D5E0);
  static const steel = Color(0xFF5B7185);

  // Estados
  static const success = Color(0xFF16A34A);
  static const warning = Color(0xFFD97706);
  static const error = Color(0xFFDC2626);

  static const white = Color(0xFFFFFFFF);
}
