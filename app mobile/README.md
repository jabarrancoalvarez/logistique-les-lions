# Yoon u Auto — App móvil (Flutter)

App Android de Yoon u Auto. **Consume la misma API que la web** (Render + Neon): no
reimplementa nada de negocio, solo el front en Flutter. Mismos datos, mismo JWT, mismos
tres usuarios y los mismos dos roles (`User` / `Admin`).

## Estado: Fase 4 — Mon Garage ✅ (fotos/documentos/transparence en 4b)

Cimientos + auth + marketplace + negociación completa + **Mon Garage**. Todo validado
(`flutter analyze` limpio, `flutter test` en verde, 13 tests):

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

> **Fase 4b (pendiente):** fotos del vehículo/intervención, documentos (subir/descargar)
> y «Transparence» (elegir qué historial se comparte en el anuncio) — necesitan selector
> de archivos.

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

- **4b** — fotos y documentos del garaje (subir/descargar) + «Transparence».
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
