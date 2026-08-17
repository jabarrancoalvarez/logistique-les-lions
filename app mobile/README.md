# Yoon u Auto — App móvil (Flutter)

App Android de Yoon u Auto. **Consume la misma API que la web** (Render + Neon): no
reimplementa nada de negocio, solo el front en Flutter. Mismos datos, mismo JWT, mismos
tres usuarios y los mismos dos roles (`User` / `Admin`).

## Estado: Fase 6 — Backoffice (admin) ✅ (núcleo; resto en 6b)

Todo lo de usuario + **backoffice de administración**. Validado (`flutter analyze`
limpio, `flutter test` en verde, 17 tests):

- **Acceso** desde Compte solo para rol Admin; ruta `/admin/**` protegida en el router.
- **Dashboard** (`GET /admin/dashboard`): usuarios, marketplace, actividad, demanda y
  garaje en cifras.
- **Utilisateurs** (`/admin/users`): búsqueda, ficha con actividad e historial, y cambio
  de estado (Actif/Suspendu/Bloqué) **con motivo obligatorio**.
- **Annonces** (`/admin/listings`): búsqueda, ficha con datos y contadores, moderación
  (masquer/réafficher/marquer/archiver/supprimer) **con motivo** y **demander une
  correction**. El admin **nunca edita** la información comercial.
- **Signalements** (`/admin/reports`): pestañas por estado, ficha con pruebas, cambio de
  estado y **avertir l’utilisateur**, todo con motivo. Enlaces a la annonce/usuario.
- Toda medida deja traza en `admin_actions` (historial visible en cada ficha).

> **Fase 6b (pendiente):** negociaciones (estructura + leer contenido **con motivo**,
> que queda registrado), contratos (solo **invalidar** con motivo), demandes,
> communications, settings/catalogs y statistics.

## Fase 5 — Notificaciones ✅

Cimientos + auth + marketplace + negociación + Mon Garage + **notificaciones en tiempo
real**:

- **Campana** con contador de no leídos **en vivo** en Accueil, Véhicules y
  Négociations (solo con sesión).
- **Tiempo real por SignalR** (`/hubs/notifications`, evento `notification`): mensajes,
  ofertas, contratos, rappels y avisos de admin llegan sin recargar y suben el contador.
- **Pantalla Notifications** (`GET /notifications`): lista con icono por categoría, leído/
  no leído, «Tout lire» (`/read-all`), marcar al abrir (`/{id}/read`) y **navegación al
  elemento** (negociación, anuncio, favoris) traduciendo el `link` del backend.

## Fase 4 — Mon Garage completo ✅

Cimientos + auth + marketplace + negociación completa + **Mon Garage** (con fotos,
documentos y transparence):

- **Mon Garage** (`features/garage`, acceso desde Compte): resumen (nº vehículos,
  rappels abiertos, valeur estimée totale) y tarjetas con complétude y próximo rappel.
- **Ficha del vehículo del garaje**: datos técnicos, compra, **valeur estimée** (rango
  + comparables + évolution) y **complétude** (score, niveau y checklist del historial,
  con el aviso de que no es un diagnóstico mecánico). Insignia «Acheté ici» y enlace al
  anuncio si está en venta.
- **Alta/edición** del vehículo (marca del catálogo, año, km, carburante, caja,
  carrocería, potencia, cilindrada, color, matrícula, VIN, compra).
- **Entretien**: historial por año con totales; alta/edición/eliminación de
  intervenciones (tipo, fecha, km, descripción, coste, atelier).
- **Rappels**: por fecha y/o kilometraje, con «en retard/dans X j·km», marcar
  terminé/annulé/rouvrir y borrar.
- **Vendre ce véhicule**: crea un **borrador** de anuncio (`/garage/{id}/sell`) y navega
  a él. ❌ Nunca publica automáticamente.
- **Fotos privadas** (4b): tira de fotos en la ficha y miniatura en la tarjeta; se
  descargan por endpoint autenticado (`image_picker` cámara/galería, byte-fetch).
- **Documents** (4b): carte grise, factures, assurance… subir (`file_picker`), abrir
  (`open_filex`) y borrar. El archivo nunca se sirve estático.
- **Transparence** (4b): al vender, se elige **casilla a casilla** qué historial se
  comparte; compartir una intervención no comparte su factura (dos casillas).

## Fase 3 — Negociación completa ✅

Cimientos + auth + marketplace + **negociación con chat en tiempo real, inspección y
contrato**:

- **Pestaña Messages** (5.ª de la barra): «Mes négociations» con sub-pestañas
  **En cours · En attente · Terminées**, contador de no leídos y última actividad.
- **Chat de la negociación** (`features/negotiations`): cronología + mensajes
  fusionados, burbujas propias/ajenas, cabecera del vehículo, **tiempo real por
  SignalR** (`/hubs/chat`, `signalr_netcore`) — mensajes en vivo, «en train
  d’écrire…» y marca de leído. Envío por REST (`/messaging/send`).
- **Ofertas**: «Faire une offre» y «Contre-offre» (modal), **aceptar/refuser** la
  oferta viva con confirmación; la cronología y el estado se refrescan solos.
- **Inspection** (3b): checklist privada de los 11 puntos (Bon/Moyen/Mauvais), fecha
  de visita, kilometraje y notas. Solo la ve su autor.
- **Contrat** (3b): ver/redactar/**modifier**/envoyer/**valider**, demander une
  modification y annuler según los permisos del backend. **Valider produce la vente
  vérifiée.** PDF del contrato (`open_filex`) y página de **vérification** en el
  navegador (`url_launcher`) desde el código QR.
- **Contacto desde la ficha**: «Contacter» y «Offre» crean/abren la negociación y
  navegan al chat. Detecta «C’est votre annonce» y anuncios vendidos.

## Fase 2 — Marketplace ✅

Escaparate de vehículos:

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

- **6b** — admin: negociaciones (contenido con motivo), contratos (invalider),
  demandes, communications, settings/catalogs, statistics.
7. **Release** — iconos, splash, firma, build y ficha de Play Store.

Pendiente de fases anteriores: comparador y demandes (Étape 1); fotos de las
intervenciones de Mon Garage (Étape 3).

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
