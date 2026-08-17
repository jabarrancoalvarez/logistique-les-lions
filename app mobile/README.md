# Yoon u Auto — App móvil (Flutter)

App Android de Yoon u Auto. **Consume la misma API que la web** (Render + Neon): no
reimplementa nada de negocio, solo el front en Flutter. Mismos datos, mismo JWT, mismos
tres usuarios y los mismos dos roles (`User` / `Admin`).

## Estado: Fase 1 — Autenticación ✅

Cimientos (Fase 0) + pantallas de sesión. Todo validado (`flutter analyze` limpio,
`flutter test` en verde, 4 tests):

- **Arquitectura** feature-first, igual que el Angular de la web.
- **Tema de marca** (`core/theme`): paleta azul océano + celeste, solo azules y blanco.
- **Conexión a la API** (`core/network/api_client.dart`) con **dio**, apuntando a la API de
  producción de Render. Interceptor que añade el JWT y, ante un 401, **refresca el token y
  reintenta** —igual que la web—.
- **Sesión** (`features/auth`): login/registro/logout, tokens **cifrados** en el Keystore
  (`flutter_secure_storage`), restauración de sesión al abrir, estado con **Riverpod**.
- **Pantalla de login** por teléfono (o correo) + contraseña, con validación y errores.
- **Pantalla de registro**: nombre, teléfono `+221`, tipo de cuenta
  (Particulier/Professionnel), **región** (las 14 de Senegal), ciudad y correo opcionales,
  contraseña. Deja la sesión iniciada al terminar.
- **Navegación con guard** (`go_router`): splash mientras se restaura la sesión, redirección
  a `/login` sin sesión y a la portada con sesión iniciada.
- **Portada post-login** que saluda al usuario, muestra su rol y comprueba en vivo la
  conexión a la API (número real de vehículos del catálogo de Senegal), con cierre de sesión.

## Fases siguientes

2. **Étape 1** — marketplace, filtros, ficha, favoritos, búsquedas, comparador, demandes.
3. **Étape 2** — negociación, chat en vivo (SignalR), ofertas, inspección, contrato, PDF, QR.
4. **Étape 3** — Mon Garage, mantenimiento, recordatorios, valor, vendre.
5. **Notificaciones** — campana + tiempo real.
6. **Admin** — backoffice en móvil.
7. **Release** — iconos, splash, firma, build y ficha de Play Store.

## Cómo ejecutarla

Requisitos de entorno (una vez):

1. **Android SDK completo** con *cmdline-tools* (vía Android Studio o el paquete
   `commandlinetools`). Hoy falta en esta máquina, y por eso aún no se genera el APK.
2. Aceptar las licencias: `flutter doctor --android-licenses`.
3. Instalar la plataforma que pide el proyecto (Android SDK Platform 37).

Luego:

```bash
cd "app mobile"
flutter pub get
flutter run                # en un emulador o móvil conectado
flutter build apk --debug  # genera el APK
```

Verificar el código sin Android:

```bash
flutter analyze
flutter test
```

## Configuración

La URL de la API está en `lib/core/config/api_config.dart`. Apunta a la misma API de
Render que la web.
