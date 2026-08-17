# Yoon u Auto — App móvil (Flutter)

App Android de Yoon u Auto. **Consume la misma API que la web** (Render + Neon): no
reimplementa nada de negocio, solo el front en Flutter. Mismos datos, mismo JWT, mismos
tres usuarios y los mismos dos roles (`User` / `Admin`).

## Estado: Fase 2 — Marketplace ✅

Cimientos + auth + **escaparate de vehículos**. Todo validado (`flutter analyze`
limpio, `flutter test` en verde, 7 tests):

- **Navegación por pestañas** (`StatefulShellRoute`): Accueil · Véhicules · Favoris ·
  Compte. El escaparate se recorre **sin sesión**, igual que la web; iniciar sesión se
  pide de forma contextual (favoritos, contacto).
- **Marketplace** (`features/vehicles`): búsqueda por texto, panel de **filtros**
  (marca, precio, año, kilometraje, región, aduana, carburante, caja, carrocería,
  estado), **ordenación**, contador de resultados, **paginación** al hacer scroll y
  pull-to-refresh. Tarjetas con foto, precio en FCFA, indicador de precio y corazón.
- **Ficha del vehículo**: galería deslizable, precio + indicador, cuadrícula de
  características, équipements, descripción, localización y **tarjeta del vendedor**
  (ventas verificadas, antigüedad, teléfono verificado). Registra la visualización.
- **Favoris**: lista de guardados (con sesión), estados vacíos y de invitado.
- **Compte**: perfil + logout con sesión; login/registro sin ella.
- Modelos y formato **FCFA** propios, mismos contratos que la API.

> El contacto real con el vendedor (chat, ofertas, contrato) llega en la **Fase 3**.

## Fase 1 — Autenticación ✅

Pantallas de sesión sobre la misma API:

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
- **Splash** mientras se restaura la sesión guardada al abrir la app.

## Fases siguientes

3. **Étape 2** — negociación, chat en vivo (SignalR), ofertas, inspección, contrato, PDF, QR.
4. **Étape 3** — Mon Garage, mantenimiento, recordatorios, valor, vendre.
5. **Notificaciones** — campana + tiempo real.
6. **Admin** — backoffice en móvil.
7. **Release** — iconos, splash, firma, build y ficha de Play Store.

Pendiente de Étape 1 para fases posteriores: comparador y demandes (búsquedas guardadas).

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
