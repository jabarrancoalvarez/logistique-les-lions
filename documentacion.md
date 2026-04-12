# Documentación Funcional — Logistique Les Lions

> Plataforma de compraventa e importación de vehículos internacionales.  
> Versión del documento: Abril 2025

---

## Índice

1. [Descripción general del sistema](#1-descripción-general-del-sistema)
2. [Roles y permisos](#2-roles-y-permisos)
3. [Modelo de datos](#3-modelo-de-datos)
4. [Mapa de rutas completo](#4-mapa-de-rutas-completo)
5. [Módulo: Página de inicio (Landing)](#5-módulo-página-de-inicio-landing)
6. [Módulo: Autenticación](#6-módulo-autenticación)
7. [Módulo: Vehículos](#7-módulo-vehículos)
8. [Módulo: Tramitación y aduanas](#8-módulo-tramitación-y-aduanas)
9. [Módulo: Mensajería](#9-módulo-mensajería)
10. [Módulo: Perfil de usuario](#10-módulo-perfil-de-usuario)
11. [Módulo: Panel de administración](#11-módulo-panel-de-administración)
12. [Módulo: Transporte internacional](#12-módulo-transporte-internacional)
13. [Módulo: Financiación](#13-módulo-financiación)
14. [Módulo: Precios](#14-módulo-precios)
15. [Módulo: Inspectores certificados](#15-módulo-inspectores-certificados)
16. [Módulo: Guías](#16-módulo-guías)
17. [Módulo: Legal](#17-módulo-legal)
18. [Módulo: Concesionarios](#18-módulo-concesionarios)
19. [Flujo de autenticación y seguridad](#19-flujo-de-autenticación-y-seguridad)
20. [API — Referencia de endpoints](#20-api--referencia-de-endpoints)
21. [Comunicación en tiempo real (SignalR)](#21-comunicación-en-tiempo-real-signalr)

---

## 1. Descripción general del sistema

Logistique Les Lions es una plataforma web de compraventa e importación/exportación de vehículos de origen internacional. Conecta a compradores, vendedores, concesionarios e importadores profesionales, y facilita además todos los servicios asociados al proceso de importación: tramitación documental, homologación, transporte y financiación.

### Stack tecnológico

| Capa | Tecnología |
|---|---|
| Frontend | Angular 19, Tailwind CSS v3, Standalone Components |
| Backend | ASP.NET Core 9, Clean Architecture + CQRS, MediatR |
| Base de datos | PostgreSQL (local: Docker, producción: Neon serverless) |
| Autenticación | ASP.NET Core Identity + JWT + Refresh Tokens |
| Tiempo real | SignalR (mensajería) |
| Despliegue | Frontend: Vercel · Backend: Render · BD: Neon |

### Principios de diseño

- **Clean Architecture**: el dominio no tiene dependencias externas; la aplicación (CQRS) depende del dominio; la infraestructura depende de la aplicación.
- **Result\<T\>**: toda la lógica de negocio devuelve `Result<T>` en lugar de lanzar excepciones.
- **Soft Delete**: ninguna entidad se elimina físicamente; el DbContext aplica filtros automáticos.
- **Slugs únicos**: los vehículos tienen URL amigable (`/vehiculos/bmw-series-3-2022`).

---

## 2. Roles y permisos

### Roles disponibles

| Rol | Valor | Descripción |
|---|---|---|
| **Buyer** | 0 | Comprador estándar. Puede buscar, contactar vendedores y gestionar favoritos. |
| **Seller** | 1 | Vendedor particular. Puede publicar vehículos además de todo lo que puede hacer un Buyer. |
| **Dealer** | 2 | Concesionario o importador profesional. Acceso a funcionalidades avanzadas de gestión de stock. Campos adicionales: nombre de empresa y NIF/CIF. |
| **Admin** | 3 | Administrador de la plataforma. Acceso total incluyendo el panel de administración. |
| **Moderator** | 4 | Reservado para futuras funciones de moderación de contenido. |

### Permisos por pantalla

| Pantalla / Ruta | Anónimo | Buyer | Seller | Dealer | Admin |
|---|---|---|---|---|---|
| Landing `/` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Listado vehículos `/vehiculos` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Detalle vehículo `/vehiculos/:slug` | ✅ ver | ✅ ver | ✅ ver/editar propio | ✅ ver/editar propio | ✅ todo |
| Publicar vehículo `/vehiculos/nuevo` | ❌→login | ✅ | ✅ | ✅ | ✅ |
| Mis vehículos `/mis-vehiculos` | ❌→login | ✅ | ✅ | ✅ | ✅ |
| Tramitación `/tramitacion` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Calculadora `/tramitacion/calculadora` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Proceso `/tramitacion/procesos/:id` | ❌→login | ✅ si involucrado | ✅ si involucrado | ✅ | ✅ |
| Mensajes `/mensajes` | ❌→login | ✅ | ✅ | ✅ | ✅ |
| Perfil `/perfil` | ❌→login | ✅ | ✅ | ✅ | ✅ |
| Panel Admin `/admin` | ❌→inicio | ❌→inicio | ❌→inicio | ❌→inicio | ✅ |
| Transporte `/transporte` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Financiación `/financiacion` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Precios `/precios` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Inspectores `/inspectores` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Guías `/guias/*` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Legal `/legal/*` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Login `/auth/login` | ✅ | ❌→inicio | ❌→inicio | ❌→inicio | ❌→inicio |
| Registro `/auth/register` | ✅ | ❌→inicio | ❌→inicio | ❌→inicio | ❌→inicio |

> **Nota**: ❌→login = redirige a `/auth/login`; ❌→inicio = redirige a `/`.

### Guards en el frontend

| Guard | Comportamiento |
|---|---|
| `authGuard` | Bloquea acceso si el usuario no está autenticado. Redirige a `/auth/login`. |
| `adminGuard` | Bloquea acceso si el usuario no tiene rol `Admin`. Redirige a `/`. |
| `guestGuard` | Bloquea acceso a páginas de auth si el usuario ya está autenticado. Redirige a `/`. |

---

## 3. Modelo de datos

### UserProfile

Entidad principal de usuario, almacenada en la base de datos vía ASP.NET Core Identity.

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | Guid | Clave primaria |
| `Email` | string | Único en la plataforma |
| `PasswordHash` | string | Hash bcrypt de la contraseña |
| `FirstName`, `LastName` | string | Nombre y apellido |
| `Phone` | string? | Teléfono (opcional) |
| `AvatarUrl` | string? | URL de foto de perfil |
| `Role` | UserRole | Enum: Buyer, Seller, Dealer, Admin, Moderator |
| `CountryCode` | string? | Código ISO de país (ES, FR, DE…) |
| `City` | string? | Ciudad |
| `IsVerified` | bool | Si el email ha sido verificado |
| `IsActive` | bool | Si la cuenta está activa (soft delete) |
| `RefreshToken` | string? | Token de renovación de sesión (opaco) |
| `RefreshTokenExpiresAt` | DateTimeOffset? | Expiración del refresh token |
| `LastLoginAt` | DateTimeOffset? | Último inicio de sesión |
| `CompanyName` | string? | Solo para Dealers |
| `CompanyVat` | string? | NIF/CIF de empresa, solo para Dealers |
| `Bio` | string? | Descripción libre del perfil |
| `CreatedAt`, `LastModifiedAt` | DateTimeOffset | Auditoría automática |

### Vehicle

Entidad central del catálogo de vehículos.

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | Guid | Clave primaria |
| `Slug` | string | URL amigable única (ej: `bmw-series-3-2022`) |
| `Title` | string | Título del anuncio (max 200 caracteres) |
| `DescriptionEs`, `DescriptionEn` | string? | Descripción en español e inglés |
| `MakeId` | Guid | FK → VehicleMake |
| `ModelId` | Guid? | FK → VehicleModel (opcional) |
| `Year` | int | Año de fabricación |
| `Mileage` | int? | Kilómetros (puede ser 0 para vehículos nuevos) |
| `Condition` | VehicleCondition | New, Used, Km0 |
| `BodyType` | BodyType? | Sedan, SUV, Coupe, Van, Truck, Minivan, Pickup… |
| `FuelType` | FuelType? | Gasoline, Diesel, Electric, Hybrid, PluginHybrid, LPG, CNG, Hydrogen |
| `Transmission` | TransmissionType? | Manual, Automatic, SemiAutomatic, CVT |
| `Color` | string? | Color del vehículo |
| `Vin` | string? | Número de bastidor (17 caracteres) |
| `Price` | decimal | Precio de venta |
| `Currency` | string | ISO 4217: EUR, USD, GBP, MAD, JPY |
| `PriceNegotiable` | bool | Si el precio es negociable |
| `CountryOrigin` | string | Código ISO del país donde está el vehículo |
| `City` | string? | Ciudad donde está el vehículo |
| `Status` | VehicleStatus | Reviewing, Active, Sold, Paused, Rejected, Expired |
| `IsFeatured` | bool | Si aparece en la landing page |
| `IsExportReady` | bool | Si tiene documentación lista para exportar |
| `Specs` | JsonDocument? | Especificaciones técnicas adicionales (potencia, emisiones…) |
| `Features` | JsonDocument? | Equipamiento (climatizador, navegador, sensores…) |
| `ViewsCount` | int | Contador de visualizaciones (desnormalizado) |
| `FavoritesCount` | int | Contador de favoritos (desnormalizado) |
| `ContactsCount` | int | Contador de contactos (desnormalizado) |
| `SellerId` | Guid | FK → UserProfile (vendedor) |
| `DealerId` | Guid? | FK → UserProfile (concesionario intermediario, si aplica) |
| `ExpiresAt` | DateTimeOffset? | Fecha de expiración del anuncio |
| `SoldAt` | DateTimeOffset? | Fecha de venta |

**Estados del vehículo**:
- `Reviewing` — Recién publicado, pendiente de aprobación por un Admin.
- `Active` — Visible públicamente en el catálogo.
- `Sold` — Marcado como vendido.
- `Paused` — Pausado temporalmente por el vendedor.
- `Rejected` — Rechazado por el Admin.
- `Expired` — El anuncio ha expirado por tiempo.

### VehicleImage

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | Guid | Clave primaria |
| `VehicleId` | Guid | FK → Vehicle |
| `Url` | string | URL de la imagen original |
| `ThumbnailUrl` | string? | URL de la miniatura |
| `SortOrder` | int | Orden de presentación |
| `IsPrimary` | bool | Si es la imagen principal del anuncio |
| `Format` | string | Formato: "webp", "jpeg", "png" |

### Conversation y Message

**Conversation**

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | Guid | Clave primaria |
| `BuyerId` | Guid | FK → UserProfile (comprador) |
| `SellerId` | Guid | FK → UserProfile (vendedor) |
| `VehicleId` | Guid | FK → Vehicle (vehículo sobre el que se inicia) |
| `IsArchivedByBuyer` | bool | Si el comprador ha archivado la conversación |
| `IsArchivedBySeller` | bool | Si el vendedor ha archivado la conversación |
| `LastMessageAt` | DateTimeOffset? | Timestamp del último mensaje (para ordenación) |

**Message**

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | Guid | Clave primaria |
| `ConversationId` | Guid | FK → Conversation |
| `SenderId` | Guid | FK → UserProfile (remitente) |
| `Body` | string | Contenido del mensaje (max 4000 caracteres) |
| `IsRead` | bool | Si el destinatario ha leído el mensaje |
| `ReadAt` | DateTimeOffset? | Cuándo fue leído |

### ImportExportProcess

Representa un proceso de importación o exportación gestionado en la plataforma.

| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | Guid | Clave primaria |
| `VehicleId` | Guid | FK → Vehicle |
| `BuyerId`, `SellerId` | Guid | Partes involucradas |
| `OriginCountry`, `DestinationCountry` | string | Códigos ISO de países |
| `ProcessType` | ProcessType | Import o Export |
| `Status` | ProcessStatus | Draft → InProgress → PendingDocuments → UnderReview → Approved → Completed (o Cancelled/Rejected) |
| `EstimatedCostEur`, `ActualCostEur` | decimal? | Coste estimado y real en EUR |
| `CompletionPercent` | int | % de completado calculado automáticamente desde los documentos |
| `StartedAt`, `CompletedAt`, `CancelledAt` | DateTimeOffset? | Fechas de ciclo de vida |

### ProcessDocument

Cada documento requerido dentro de un proceso de importación/exportación.

| Campo | Tipo | Descripción |
|---|---|---|
| `DocumentType` | string | COC, DUA, FACTURA_COMPRA, ITV, HOMOLOGACION, etc. |
| `Status` | DocumentStatus | Pending, Uploaded, Verified, Rejected, NotRequired |
| `ResponsibleParty` | string | "buyer" o "seller" |
| `FileUrl` | string? | URL del documento subido |
| `TemplateUrl` | string? | URL de la plantilla oficial descargable |
| `OfficialUrl` | string? | URL de la autoridad oficial |
| `EstimatedCostEur` | decimal? | Coste estimado de obtención |

### CountryRequirement

Normativa aplicable para cada par origen-destino.

| Campo | Tipo | Descripción |
|---|---|---|
| `OriginCountry`, `DestinationCountry` | string | Par de países (ej: DE→ES) |
| `DocumentTypesJson` | string | Array JSON de tipos de documentos requeridos |
| `HomologationRequired` | bool | Si se requiere homologación |
| `CustomsRatePercent` | decimal | Arancel aduanero (0% para intra-UE) |
| `VatRatePercent` | decimal | IVA aplicable (21% en España) |
| `EstimatedProcessingCostEur` | decimal | Coste total estimado de tramitación |
| `EstimatedDays` | int | Días estimados para completar el proceso |

---

## 4. Mapa de rutas completo

```
/ ─────────────────────────── Landing page (pública)
│
├─ /auth/
│   ├─ login ──────────────── Inicio de sesión (solo anónimos)
│   └─ register ───────────── Registro de cuenta (solo anónimos)
│
├─ /vehiculos/
│   ├─ (raíz) ─────────────── Listado con filtros (público)
│   ├─ nuevo ──────────────── Formulario de publicación (autenticado)
│   ├─ publicar ───────────── Redirect → /vehiculos/nuevo
│   └─ :slug ──────────────── Detalle del vehículo (público)
│
├─ /tramitacion/
│   ├─ (raíz) ─────────────── Consulta de requisitos por país (público)
│   ├─ calculadora ─────────── Calculadora de coste de importación (público)
│   └─ procesos/:id ────────── Seguimiento de proceso documental (autenticado)
│
├─ /mensajes/
│   ├─ (raíz) ─────────────── Bandeja de entrada (autenticado)
│   └─ :id ────────────────── Conversación individual (autenticado)
│
├─ /mis-vehiculos ─────────── Vehículos publicados por el usuario (autenticado)
│
├─ /perfil ─────────────────── Edición de perfil de usuario (autenticado)
│
├─ /concesionarios ─────────── Directorio de concesionarios (público)
│
├─ /admin ──────────────────── Panel de administración (solo Admin)
│
├─ /precios ────────────────── Planes y tarifas (público)
│
├─ /calculadora ────────────── Redirect → /tramitacion/calculadora
│
├─ /inspectores ────────────── Directorio de inspectores certificados (público)
│
├─ /transporte ─────────────── Servicios de transporte internacional (público)
│
├─ /financiacion ───────────── Opciones de financiación (público)
│
├─ /logistica ──────────────── Redirect → /transporte
│
├─ /guias/
│   ├─ (raíz) ─────────────── Redirect → /guias/importacion
│   ├─ importacion ─────────── Guía de importación (público)
│   ├─ exportacion ─────────── Guía de exportación (público)
│   └─ homologacion ────────── Guía de homologaciones UE (público)
│
├─ /legal/
│   ├─ (raíz) ─────────────── Redirect → /legal/aviso-legal
│   ├─ aviso-legal ─────────── Aviso legal (público)
│   ├─ privacidad ──────────── Política de privacidad (público)
│   ├─ cookies ─────────────── Política de cookies (público)
│   ├─ terminos ────────────── Términos y condiciones (público)
│   └─ rgpd ───────────────── Protección de datos RGPD (público)
│
├─ /pagos ──────────────────── Próximamente
├─ /valoraciones ───────────── Próximamente
│
└─ ** ──────────────────────── 404 Not Found
```

---

## 5. Módulo: Página de inicio (Landing)

**Ruta**: `/`  
**Acceso**: Público

La landing page es el punto de entrada principal de la plataforma. Está compuesta por varios subcomponentes que se cargan de forma progresiva.

### Hero con buscador

- Muestra un banner principal con el eslogan de la plataforma sobre un fondo oscuro con imagen de vehículo.
- Incluye un formulario de búsqueda rápida que permite filtrar por marca/modelo.
- Al hacer clic en "Buscar" navega a `/vehiculos` con los parámetros de búsqueda aplicados.
- En la parte inferior hay una transición visual (ola SVG) hacia la siguiente sección.

### Contadores estadísticos

Muestra 4 métricas clave de la plataforma con animación de conteo:
- Vehículos activos en el catálogo.
- Países desde donde se pueden importar vehículos.
- Transacciones completadas en la plataforma.
- Concesionarios registrados.

Datos obtenidos del endpoint `GET /api/v1/vehicles/stats` con caché de 5 minutos.

### Vehículos destacados

- Grid de hasta 6 vehículos marcados como `IsFeatured = true`.
- Cada tarjeta muestra: imagen principal, marca, título, año, kilometraje, combustible, precio, divisa, días desde publicación y contador de visualizaciones.
- Al hacer clic en una tarjeta navega al detalle del vehículo.
- El botón de favorito (corazón) en la esquina superior derecha de cada tarjeta permite guardar el vehículo (estado local, no persistido en esta versión del frontend).
- Si no hay imagen disponible o falla la carga, se muestra un placeholder con icono de cámara.

### Cómo funciona

Sección con 4 pasos del proceso de importación, cada uno con:
- Número de paso en círculo dorado.
- Icono representativo.
- Título y descripción breve.
Los pasos son: Busca tu vehículo → Consulta requisitos → Gestiona la tramitación → Recibe tu vehículo.

### Mapa de países

Sección visual que muestra los países desde los que la plataforma gestiona importaciones, con banderas emoji y cobertura de cobertura.

### Testimonios

Grid de testimonios de clientes reales (datos estáticos de muestra) con:
- Avatar, nombre, rol (comprador/dealer), texto del testimonio y valoración en estrellas.
- Hover con borde dorado.

### CTA de concesionarios

Banner oscuro con llamada a la acción para registrarse como concesionario, con elementos visuales decorativos (leones).

### Newsletter

Formulario de suscripción a la newsletter:
- Campo de email con validación.
- Envía una petición `POST /api/v1/newsletter/subscribe`.
- Muestra mensaje de confirmación o error.

### Consentimiento de cookies

Banner persistente en la parte inferior con política de cookies. Desaparece al aceptar y no vuelve a mostrarse (guardado en localStorage).

---

## 6. Módulo: Autenticación

### Inicio de sesión — `/auth/login`

**Acceso**: Solo usuarios no autenticados (guestGuard)

**Campos del formulario**:
- Email (requerido, formato válido)
- Contraseña (requerido, mínimo 8 caracteres)

**Flujo**:
1. Usuario rellena email y contraseña.
2. Clic en "Iniciar sesión" → llama a `POST /api/v1/auth/login`.
3. Si éxito: guarda `accessToken`, `refreshToken` y datos de usuario en `localStorage`. Redirige a `/`.
4. Si error: muestra mensaje de error del servidor (credenciales incorrectas, cuenta inactiva, etc.).

**Estado de pantalla**: Señales `loading` (desactiva el botón) y `error` (muestra alerta roja).  
**Enlace adicional**: "¿No tienes cuenta? Regístrate" → navega a `/auth/register`.

---

### Registro — `/auth/register`

**Acceso**: Solo usuarios no autenticados (guestGuard)

**Campos del formulario**:
- Email (requerido, formato válido)
- Contraseña (requerido, mínimo 8 caracteres)
- Nombre y apellido (requeridos)
- Rol (selector: Comprador, Vendedor, Concesionario)
- Teléfono (opcional)
- Código de país (opcional, ISO)

**Campos adicionales para Concesionarios**:
- Nombre de empresa (requerido si rol = Dealer)
- NIF/CIF de la empresa (requerido si rol = Dealer)

**Flujo**:
1. Usuario rellena todos los campos.
2. Clic en "Crear cuenta" → llama a `POST /api/v1/auth/register`.
3. Si éxito: guarda tokens y datos de usuario. Redirige a `/`.
4. Si error: muestra mensaje de error (email ya en uso, etc.).

---

## 7. Módulo: Vehículos

### Listado de vehículos — `/vehiculos`

**Acceso**: Público

**Descripción**: Catálogo completo de vehículos con búsqueda avanzada y paginación.

**Panel de filtros** (desplegable en móvil, visible en desktop):
- **Texto de búsqueda**: busca en título, marca y descripción.
- **Marca**: selector con todas las marcas disponibles (cargadas desde la API).
- **País de origen**: selector con países soportados.
- **Rango de precio**: desde / hasta (inputs numéricos).
- **Rango de año**: desde / hasta.

**Opciones de ordenación**:
- Más recientes (por fecha de publicación DESC)
- Precio: menor a mayor
- Precio: mayor a menor
- Año: más nuevo primero
- Menos kilómetros
- Más vistos

**Paginación**: controles de primera/última página + rango de páginas cercanas (±2 al actual). Se mantiene el número de página en los query params de la URL.

**Tarjeta de vehículo** (VehicleCardComponent):  
Componente compartido usado también en la landing page. Muestra:
- Imagen con lazy loading. Si falla la carga, placeholder con icono de cámara y nombre de marca.
- Badge de condición (Nuevo / Ocasión / Km 0).
- Botón de favorito (corazón) en esquina superior derecha.
- Badge de país de origen con bandera emoji (visible en hover).
- Marca, título, año, kilometraje formateado, tipo de combustible.
- Precio formateado con separador de miles, divisa.
- Días desde publicación (Hoy / Ayer / Hace N días).
- Contador de visitas.
- Al hacer clic navega a `/vehiculos/:slug`.

---

### Detalle de vehículo — `/vehiculos/:slug`

**Acceso**: Público para ver; autenticado para contactar al vendedor.

**Layout**:
- **Columna izquierda (desktop)**: galería de imágenes.
  - Imagen activa grande.
  - Miniaturas horizontales para seleccionar imagen activa.
- **Columna derecha (desktop)**: información y acciones.
  - Badge de condición, badge de estado.
  - Título, precio, divisa.
  - Especificaciones: año, kilómetros, combustible, transmisión, carrocería, color, VIN, país de origen, ciudad.
  - Sección "Descripción" (si existe).
  - Botón "Contactar con el vendedor":
    - Si el usuario no está autenticado → redirige a `/auth/login`.
    - Si está autenticado → crea una nueva conversación con el vendedor via `POST /api/v1/messaging/send` → navega a `/mensajes/:conversationId`.
  - Botón de favorito.

**Estado de la pantalla**:
- `isLoading` → muestra skeleton placeholder mientras carga.
- `error` → muestra mensaje de error con enlace para volver al listado.
- `contacting` → desactiva el botón mientras se crea la conversación.

---

### Publicar vehículo — `/vehiculos/nuevo`

**Acceso**: Autenticado (cualquier rol)

**Descripción**: Formulario de 4 pasos para publicar un nuevo anuncio.

**Paso 1 — Datos básicos**:
- Marca (selector cargado desde API, requerido)
- Modelo (selector dependiente de la marca, opcional)
- Año de fabricación (selector 1980–año actual, requerido)
- Condición: Nuevo, Ocasión, Km 0 (por defecto: Ocasión)
- VIN / Número de bastidor (texto, opcional, validación 17 caracteres si se rellena)

**Paso 2 — Especificaciones**:
- Carrocería: Sedan, Hatchback, SUV, Coupé, Descapotable, Familiar, Furgoneta, Camión, Monovolumen, Pick-up
- Combustible: Gasolina, Diésel, Eléctrico, Híbrido, Híbrido enchufable, GLP, GNC, Hidrógeno
- Transmisión: Manual, Automático, Semiautomático, CVT
- Kilometraje (número entero)
- Color

**Paso 3 — Precio y ubicación**:
- Precio (número, requerido)
- Divisa: EUR, USD, GBP, MAD, JPY
- ¿Precio negociable? (checkbox)
- País donde está el vehículo (selector ISO, requerido)
- Ciudad
- ¿Listo para exportar? (checkbox)

**Paso 4 — Fotos y descripción**:
- Título del anuncio (texto, requerido, max 200 caracteres)
- Descripción en español (textarea, max 5000 caracteres)

**Comportamiento de navegación entre pasos**:
- El botón "Siguiente" valida solo los campos del paso actual antes de avanzar.
- El botón "Volver" retrocede sin perder los datos.
- Una barra de progreso muestra el avance (0%, 33%, 66%, 100%).
- Al enviar en el paso 4: `POST /api/v1/vehicles` → si éxito, navega a `/vehiculos`.

---

### Mis vehículos — `/mis-vehiculos`

**Acceso**: Autenticado (authGuard)

**Descripción**: Vista de gestión de los vehículos publicados por el usuario actual.

**Columnas de la tabla**:
- Título del anuncio
- Marca
- Año
- Precio y divisa
- Estado con badge coloreado:
  - 🟡 **En revisión** (Reviewing)
  - 🟢 **Activo** (Active)
  - 🔵 **Vendido** (Sold)
  - 🔴 **Expirado** (Expired)
  - ⚫ **Inactivo** (Inactive)
- Acciones: enlace al detalle del vehículo.

**Datos**: llamada a `GET /api/v1/vehicles?sellerId={userId}`.

---

## 8. Módulo: Tramitación y aduanas

### Consulta de requisitos — `/tramitacion`

**Acceso**: Público

**Descripción**: Herramienta para consultar los requisitos legales y documentales de importación/exportación entre dos países.

**Controles**:
- Selector de país de origen (España, Alemania, Francia, Italia, Japón, EE.UU., Reino Unido, Marruecos)
- Selector de país de destino (misma lista)
- Botón "Consultar requisitos"

**Resultado mostrado** (tras la búsqueda):
- Lista de documentos requeridos para el proceso.
- Si se requiere homologación (sí/no).
- Arancel aduanero aplicable (%) — 0% para movimientos intra-UE.
- IVA aplicable (%).
- Días estimados para completar el proceso.
- Coste total estimado en EUR.

**Sección informativa estática**:
4 tarjetas con los pasos del proceso: Consulta requisitos → Obtén el checklist → Calcula costes → Gestiona el proceso.

**Fuente de datos**: `GET /api/v1/compliance/requirements?origin=XX&destination=YY` (caché 1 hora).

---

### Calculadora de importación — `/tramitacion/calculadora`

**Acceso**: Público  
**Ruta alternativa**: `/calculadora` (redirect)

**Descripción**: Calcula el coste total estimado de importar un vehículo específico entre dos países.

**Controles**:
- ID del vehículo (campo de texto — puede recibirse por query param `?vehicleId=...`)
- País de origen (selector)
- País de destino (selector)
- Botón "Calcular"

**Resultado mostrado** (desglose de costes):
- Precio del vehículo (EUR)
- Derechos aduaneros (EUR + %)
- IVA (EUR + %)
- Tasas de tramitación (EUR)
- Coste de homologación (EUR, si aplica)
- **Total estimado** (EUR) — resaltado

Si el movimiento es intra-UE, se indica que no hay aranceles aduaneros.

**Fuente de datos**: `GET /api/v1/compliance/estimate?vehicleId=...&origin=XX&destination=YY` (caché 15 min).

---

### Seguimiento de proceso — `/tramitacion/procesos/:id`

**Acceso**: Autenticado; solo accesible por las partes involucradas en el proceso (comprador, vendedor) o Admin.

**Descripción**: Seguimiento en tiempo real del estado de un proceso de importación/exportación activo.

**Cabecera del proceso**:
- Título del vehículo, par origen-destino, tipo de proceso (Importación/Exportación).
- Badge del estado global: Borrador, En proceso, Pendiente documentos, En revisión, Aprobado, Completado, Cancelado, Rechazado.
- Barra de progreso (0–100%) calculada automáticamente según documentos completados.

**Lista de documentos**:  
Para cada documento del proceso:
- Tipo de documento (COC, DUA, Factura de compra, ITV, Homologación, etc.)
- Badge de estado: Pendiente / Subido / Verificado / Rechazado / No requerido.
- Responsable: comprador o vendedor.
- Fecha límite (si está definida).
- Enlace de descarga del documento subido (si existe).
- Enlace a la plantilla oficial descargable.
- Enlace a la autoridad oficial donde obtenerlo.
- Coste estimado de obtención.

---

## 9. Módulo: Mensajería

### Bandeja de entrada — `/mensajes`

**Acceso**: Autenticado (authGuard)

**Descripción**: Lista de todas las conversaciones del usuario, ordenadas por fecha del último mensaje.

**Por cada conversación se muestra**:
- Nombre del otro usuario.
- Título del vehículo al que se refiere la conversación (con miniatura si está disponible).
- Último mensaje (preview truncado).
- Timestamp del último mensaje (fecha/hora relativa).
- Badge con número de mensajes no leídos.

**Al hacer clic** en una conversación navega a `/mensajes/:id`.

**Fuente de datos**: `GET /api/v1/messaging/conversations`.

---

### Conversación — `/mensajes/:id`

**Acceso**: Autenticado (authGuard); solo los participantes de esa conversación.

**Descripción**: Vista de chat en tiempo real para una conversación sobre un vehículo específico.

**Layout de mensajes**:
- Mensajes del usuario actual alineados a la derecha (fondo azul oscuro).
- Mensajes del otro usuario alineados a la izquierda (fondo blanco/gris).
- Cada mensaje muestra: nombre del remitente, cuerpo del mensaje, hora de envío, indicador de leído.
- La vista se desplaza automáticamente al mensaje más reciente.

**Composición de mensajes**:
- Área de texto multi-línea (max 4000 caracteres).
- Botón "Enviar".
- Al enviar: llama a `POST /api/v1/messaging/send` con el `body` y el `conversationId`. El mensaje se añade optimistamente a la lista.

**Tiempo real**:
- Al entrar en la conversación se inicia una conexión SignalR al hub `/hubs/chat` con el JWT del usuario.
- Se llama al método `JoinConversation(conversationId)` para unirse a la sala.
- Cuando llega un nuevo mensaje del otro usuario (`ReceiveMessage` event), la lista de mensajes se recarga automáticamente.
- Al salir de la vista se llama a `LeaveConversation(conversationId)`.

---

## 10. Módulo: Perfil de usuario

### Edición de perfil — `/perfil`

**Acceso**: Autenticado (authGuard)

**Descripción**: Permite al usuario ver y editar su información personal.

**Campos editables**:
- Nombre y apellido
- Teléfono
- Ciudad
- Nombre de empresa y NIF/CIF (solo visible para Dealers)
- Bio / descripción libre
- URL de avatar

**Campos no editables** (solo visualización):
- Email
- Rol
- Fecha de registro
- Estado de verificación

**Al guardar**: llama a `PUT /api/v1/auth/me`. Si éxito, actualiza los datos locales del usuario en `AuthService` y en `localStorage`.

**Información de la sesión**:
- El navbar (barra de navegación superior) muestra el nombre y avatar del usuario cuando está autenticado.
- El dropdown del avatar en el navbar incluye accesos rápidos a: Mi perfil, Mis vehículos, Panel Admin (solo admins), Cerrar sesión.

---

## 11. Módulo: Panel de administración

### Dashboard — `/admin`

**Acceso**: Solo usuarios con rol `Admin` (adminGuard)

**Descripción**: Panel centralizado de gestión y monitorización de la plataforma.

#### Sección de estadísticas

8 tarjetas con métricas globales:
- Total de vehículos publicados (todas las épocas).
- Anuncios activos en este momento.
- Total de usuarios registrados.
- Nuevos usuarios registrados este mes.
- Procesos de importación/exportación activos.
- Procesos completados.
- Total de conversaciones abiertas.
- Valor total de los anuncios activos en EUR.

#### Tabla de gestión de vehículos

**Filtro de estado** (dropdown): Todos / En revisión / Activos / Vendidos / Expirados / Inactivos.

**Columnas**:
- Título del anuncio.
- Slug (URL amigable).
- Estado (badge coloreado).
- Precio y divisa.
- Email del vendedor.
- Marca.
- Año.
- Fecha de publicación.
- Fecha de expiración.

**Acciones por fila**:
- **Aprobar**: visible solo para vehículos en estado `Reviewing`. Llama a `POST /api/v1/admin/vehicles/{id}/approve` → cambia el estado a `Active`. La tabla se recarga automáticamente.

**Paginación**: controles Anterior / Siguiente + indicador "Página X de Y". 20 vehículos por página.

---

## 12. Módulo: Transporte internacional

### `/transporte`  
**Ruta alternativa**: `/logistica` (redirect)  
**Acceso**: Público

**Descripción**: Página completa de los servicios de transporte de vehículos de la plataforma.

#### Estadísticas del servicio (sección hero)

- +50 países de destino cubiertos.
- 2.400+ vehículos transportados.
- 4,9/5 de valoración media.
- 100% de envíos con seguimiento incluido.

#### Tres modalidades de transporte

**1. Transporte en camión (Europa)**:
- Portacoches cerrado (premium) y abierto (estándar).
- Cobertura: toda Europa, entrega puerta a puerta.
- Plazo: 2–7 días laborables.
- GPS en tiempo real en cada camión.

**2. Contenedor marítimo (Intercontinental)**:
- Contenedor 20ft individual (1 vehículo) o RoRo compartido (opción económica).
- Gestión aduanera incluida en el precio.
- Bill of Lading certificado.
- Plazo: 10–35 días según destino.

**3. Transporte express**:
- Recogida garantizada en 24 horas.
- Conductor dedicado exclusivo al vehículo.
- Entrega en 1–3 días en la UE.
- Comunicación directa con el conductor.

#### Tabla de rutas orientativas

8 rutas con precio desde, método de transporte y plazo estimado:

| Origen | Destino | Método | Plazo | Precio desde |
|---|---|---|---|---|
| Alemania | España | Camión | 3–5 días | 450€ |
| Francia | España | Camión | 2–3 días | 320€ |
| Italia | España | Camión | 3–4 días | 390€ |
| Reino Unido | España | Camión | 4–6 días | 580€ |
| Japón | España | Contenedor | 28–35 días | 1.200€ |
| EE.UU. | España | RoRo | 18–25 días | 950€ |
| Marruecos | España | Ferry + Camión | 3–5 días | 380€ |
| Polonia | España | Camión | 4–6 días | 520€ |

#### Seguimiento en tiempo real

Panel visual que simula el tracker de un envío con eventos secuenciales:
- Vehículo recogido en origen.
- En tránsito (paso fronterizo).
- En tránsito (ciudad intermedia).
- Entrega en destino (estimado).

Características del servicio de tracking:
- Actualizaciones automáticas por email y SMS.
- Localización GPS del transportista.
- Notificación en cada evento de la ruta.
- Acceso desde móvil y ordenador.
- Historial completo descargable.

#### Seguro de transporte incluido

Todos los envíos incluyen seguro a todo riesgo hasta el valor declarado del vehículo. En caso de siniestro, la plataforma gestiona la reclamación y compensa en un máximo de 30 días.

#### Proceso de solicitud

1. Solicitud online con datos del vehículo y destino.
2. Presupuesto personalizado en menos de 2 horas.
3. Recogida del vehículo en la dirección acordada.
4. Entrega con firma y verificación del estado.

---

## 13. Módulo: Financiación

### `/financiacion`  
**Acceso**: Público

**Descripción**: Página completa de opciones de financiación para la adquisición de vehículos importados.

#### Estadísticas destacadas

- TIN desde el 3,9%.
- Plazo máximo de 84 meses.
- Respuesta en 48 horas.
- 100% online, sin desplazamientos.

#### Tres planes de financiación

**1. Financiación Clásica** (préstamo personal):
- TIN desde 5,9%.
- Plazo: 12 a 84 meses.
- Importe mínimo: 5.000€.
- Sin entrada obligatoria, sin restricción de marca ni modelo. Vehículos nuevos y de ocasión. Amortización anticipada sin penalización.

**2. Financiación Premium** (con VRG — Valor Residual Garantizado):
- TIN desde 3,9%.
- Plazo: 24 a 60 meses.
- Importe mínimo: 15.000€.
- Cuotas hasta un 30% más bajas gracias al VRG. Opción de compra al final del contrato, extensión o devolución.

**3. Leasing Empresarial**:
- TIN desde 4,5%.
- Plazo: 24 a 60 meses.
- Importe mínimo: 10.000€.
- IVA deducible al 100%. Cuotas como gasto deducible. Gestión de matriculación incluida.

#### Calculadora de cuota mensual (interactiva)

4 sliders configurables en tiempo real:
- **Precio del vehículo**: 5.000€ — 200.000€ (paso de 1.000€).
- **Entrada**: 0% — 50% del precio (paso del 5%).
- **Plazo**: 12 — 84 meses (paso de 12 meses).
- **TIN**: 3% — 12% (paso de 0,5%).

**Resultado en tiempo real**:
- Cuota mensual calculada con la fórmula de amortización francesa.
- Desglose: precio, entrada, capital a financiar, total de intereses, coste total.

> Fórmula aplicada: `P * (r * (1 + r)^n) / ((1 + r)^n - 1)` donde P = capital a financiar, r = TIN mensual, n = número de cuotas.

#### Requisitos para particulares

- DNI o NIE en vigor.
- Últimas 3 nóminas (o declaración de la renta para autónomos).
- Informe de vida laboral (menos de 3 meses de antigüedad).
- Sin incidencias en la CIRBE ni en ficheros de morosidad.

#### Requisitos para empresas

- CIF de la empresa y DNI del representante legal.
- Últimas 2 declaraciones anuales (IS o IRPF).
- Balance y cuenta de resultados del último ejercicio.
- Empresa con antigüedad mínima de 1 año.

#### Proceso de solicitud

1. Solicitud online (menos de 5 minutos).
2. Análisis del perfil en 48 horas laborables.
3. Firma digital del contrato (sin desplazamientos).
4. Fondos disponibles en 48 horas tras la firma.

#### Entidades financieras colaboradoras

Santander Consumer, CaixaBank, BBVA Autorenting, Sabadell, Cetelem, Bankinter.

---

## 14. Módulo: Precios

### `/precios`  
**Acceso**: Público

**Descripción**: Página de planes de suscripción y tarifas de servicios adicionales.

#### Tres planes de acceso a la plataforma

**Particular** (Gratis):
- Hasta 3 anuncios activos simultáneamente.
- Búsqueda avanzada con todos los filtros.
- Contacto con vendedores.
- Guardado de favoritos.
- Alertas de nuevos vehículos.
- Soporte por email.

**Profesional** (49€/mes):
- Todo lo anterior, sin límite de anuncios.
- Posición destacada en resultados de búsqueda.
- Acceso a la calculadora de importación avanzada.
- Seguimiento de procesos en tiempo real.
- Estadísticas detalladas de cada anuncio.
- Soporte prioritario por chat.
- Badge de vendedor verificado en el perfil.
- Prueba gratuita de 14 días.

**Concesionario** (199€/mes):
- Todo lo del plan Profesional.
- Página de perfil de concesionario personalizada.
- API para importación masiva de stock.
- Hasta 10 usuarios por cuenta.
- Informes mensuales de mercado.
- Gestor de cuenta dedicado.
- Integraciones con CRM externo.

#### Tarifas de servicios adicionales

| Servicio | Precio |
|---|---|
| Tramitación documental básica (intra-UE) | 299€ |
| Tramitación documental extra-UE | 499€ |
| Homologación individual | Desde 800€ |
| Gestión aduanera completa | 350€ |
| Inspección pre-compra | 150€ |
| Transporte EU → España | Desde 450€ |

#### Preguntas frecuentes

- Cambio de plan en cualquier momento (actualización inmediata, cancelación hasta fin del período).
- Los servicios de tramitación son adicionales a los planes mensuales.
- Sin comisión sobre transacciones.
- Descuento del 20% en pago anual.

---

## 15. Módulo: Inspectores certificados

### `/inspectores`  
**Acceso**: Público

**Descripción**: Directorio de inspectores profesionales certificados por la plataforma para realizar revisiones técnicas de vehículos in situ.

#### Propuesta de valor

- **Informe de 150 puntos**: revisión mecánica, carrocería, historial de accidentes y documentación.
- **Presencia in situ**: el inspector acude al vehículo, no al revés. Cobertura en toda Europa.
- **Sin conflicto de interés**: los inspectores trabajan para el comprador, no para el vendedor.

#### Directorio de inspectores

Actualmente la plataforma cuenta con 6 inspectores certificados:

| Nombre | Ubicación | País | Especialidades | Valoración |
|---|---|---|---|---|
| Hans Mueller | Munich | Alemania | BMW, Mercedes, Audi | 5/5 (127 reseñas) |
| Jean-Pierre Dupont | Lyon | Francia | Peugeot, Renault, DS | 5/5 (89 reseñas) |
| Marco Rossi | Milano | Italia | Ferrari, Maserati, Alfa Romeo | 4/5 (63 reseñas) |
| Carlos Ferreira | Lisboa | Portugal | Multimarca, Eléctricos | 5/5 (45 reseñas) |
| Mikael Lindqvist | Estocolmo | Suecia | Volvo, Koenigsegg | 4/5 (38 reseñas) |
| Piotr Kowalski | Varsovia | Polonia | Multimarca, Usados | 4/5 (71 reseñas) |

Cada tarjeta muestra: nombre, ubicación, badge "Certificado", estrellas, número de valoraciones, especialidades, y botón "Solicitar inspección" (navega a `/tramitacion`).

#### Proceso de inspección

1. Solicitud online seleccionando el vehículo y el inspector más cercano.
2. El inspector acude al vehículo en 24–48 horas.
3. Informe técnico detallado con fotos entregado en 24 horas.
4. El comprador decide con total información.

#### Precio

El servicio de inspección pre-compra tiene un coste de **150€** (también listado en la página de precios).

---

## 16. Módulo: Guías

Las guías son páginas informativas estáticas que explican los procesos de importación, exportación y homologación en detalle.

### Guía de importación — `/guias/importacion`

**Datos del proceso**:
- Duración estimada: 4–8 semanas.
- Complejidad: Media.

**7 pasos del proceso**:

1. **Verificación del vehículo**: historial, estado mecánico, cargas pendientes, informe provisional de homologación.
2. **Documentación de compra**: factura, título de propiedad, certificado de conformidad CE (si es UE), permiso de circulación.
3. **Despacho aduanero**: presentación en aduana, pago de aranceles (6,5% para vehículos de pasajeros de fuera de la UE).
4. **Pago de IVA e impuestos**: IVA 21% + Impuesto de Matriculación (0–14,75% según emisiones CO₂).
5. **Homologación**: laboratorio autorizado, comprobación de normativas técnicas españolas y europeas.
6. **ITV de importación**: inspección técnica de primera matriculación.
7. **Matriculación**: presentación en Jefatura Provincial de Tráfico.

**Documentos necesarios**: Factura de compraventa, título de propiedad, certificado de conformidad CE, DUA, justificante de aranceles e IVA, informe de homologación del Ministerio, ficha técnica, DNI/NIE.

---

### Guía de exportación — `/guias/exportacion`

**Datos del proceso**:
- Duración estimada: 2–6 semanas.
- Complejidad: Baja-Media.

**5 pasos del proceso**:
1. Preparación documental (permiso de circulación, ficha técnica, verificar ausencia de cargas).
2. Baja de matrícula española en Jefatura Provincial de Tráfico.
3. Despacho de exportación (DUA si destino fuera de la UE, posible devolución de IVA).
4. Transporte internacional (camión portacoches o contenedor marítimo, seguro obligatorio).
5. Documentación en destino para el comprador.

---

### Guía de homologaciones UE — `/guias/homologacion`

**Datos del proceso**:
- Duración estimada: 2–12 semanas.
- Complejidad: Alta.

**6 pasos del proceso**:
1. Evaluación previa (tipo de homologación necesaria según origen).
2. Solicitud al Ministerio de Industria, Comercio y Turismo.
3. Inspección técnica por laboratorio oficial.
4. Adaptaciones necesarias (luces, limitador de velocidad, escape…).
5. Certificado de homologación + ficha técnica reducida.
6. ITV de primera matriculación.

**Nota sobre costes**: la homologación individual puede costar entre 500€ y 5.000€ según el vehículo y las adaptaciones necesarias.

---

## 17. Módulo: Legal

Cinco páginas legales accesibles desde el footer de la plataforma.

### Aviso Legal — `/legal/aviso-legal`

Contiene: datos de identificación de la empresa titular (Logistique Les Lions, S.L.), objeto y ámbito de aplicación del sitio web, propiedad intelectual e industrial, exclusión de garantías y responsabilidad, legislación aplicable y jurisdicción (Juzgados de Madrid).

### Política de Privacidad — `/legal/privacidad`

Contiene: responsable del tratamiento, categorías de datos recopilados (identificativos, contacto, uso, técnicos), finalidades del tratamiento, base jurídica (RGPD art. 6), período de conservación, derechos del usuario (acceso, rectificación, supresión, oposición, portabilidad, limitación), transferencias internacionales y salvaguardas aplicadas.

**Ejercicio de derechos**: escribiendo a `privacidad@logistiqueleslions.com`.

### Política de Cookies — `/legal/cookies`

Contiene: qué son las cookies, cookies estrictamente necesarias (sesión, CSRF, preferencias), cookies analíticas (Google Analytics, datos anónimos y agregados), cookies de funcionalidad (preferencias de idioma y región), instrucciones para gestionar y eliminar cookies desde el navegador.

### Términos y Condiciones — `/legal/terminos`

Contiene: descripción del servicio (intermediación, no parte en transacciones), registro y responsabilidad del usuario por sus credenciales, reglas de publicación de anuncios (veracidad, prohibición de fraude), tarifas y política de pagos (sin comisión por transacción), limitación de responsabilidad de la plataforma sobre el estado real de los vehículos, y política de modificación de términos.

### Protección de Datos (RGPD) — `/legal/rgpd`

Contiene: bases de legitimación para cada tratamiento, categorías de datos tratados (no se tratan datos especialmente protegidos), destinatarios y cesiones de datos, plazos de conservación por tipo de obligación legal, derechos de los interesados y cómo ejercerlos, datos de contacto del Delegado de Protección de Datos (DPO): `dpo@logistiqueleslions.com`.

---

## 18. Módulo: Concesionarios

### `/concesionarios`  
**Acceso**: Público

**Estado actual**: Página informativa que anuncia el directorio de concesionarios como funcionalidad próxima.

**Contenido**:
- Explicación del directorio de concesionarios verificados.
- CTA para que concesionarios se registren.
- Enlace para ver los vehículos disponibles actualmente.

**Estado futuro previsto**: directorio con ficha de cada concesionario (nombre, ubicación, especialidades, vehículos en stock, valoraciones de compradores).

---

## 19. Flujo de autenticación y seguridad

### Ciclo de vida de la sesión

```
1. Usuario hace login/register
      ↓
2. Backend devuelve { accessToken (JWT), refreshToken (opaco), user }
      ↓
3. Frontend guarda en localStorage:
   - lll_access_token  (JWT con expiración ~15-60 min)
   - lll_refresh_token (opaco, expira en ~7-30 días)
   - lll_user          (datos del usuario serializados)
      ↓
4. Cada petición HTTP incluye:
   Authorization: Bearer {accessToken}   ← añadido por AuthInterceptor
      ↓
5. Si el servidor responde 401 (token expirado):
   ErrorInterceptor llama a POST /api/v1/auth/refresh
      ↓
6. Backend genera nuevo accessToken
      ↓
7. Frontend actualiza localStorage y reintenta la petición original
      ↓
8. En logout: POST /api/v1/auth/logout → backend invalida refreshToken en BD
   → Frontend limpia localStorage y señales de AuthService
```

### Tokens

| Token | Tipo | Almacenamiento | Expiración |
|---|---|---|---|
| Access Token | JWT firmado | `localStorage` | ~15–60 minutos |
| Refresh Token | Opaco (GUID) | `localStorage` + BD | ~7–30 días |

### Protección de endpoints (backend)

- Endpoints públicos: acceso libre, sin cabecera de autorización.
- Endpoints autenticados: requieren `[Authorize]` → validan JWT Bearer.
- Endpoints de admin: requieren `[Authorize(Policy = "AdminOnly")]` → validan JWT + comprueban `UserRole.Admin`.
- Endpoints de autenticación (login, register): `RequireRateLimiting("AuthRateLimit")` para prevenir ataques de fuerza bruta.

### Protección de rutas (frontend)

Los guards se ejecutan antes de cargar cada componente de ruta. Si el guard falla, Angular redirige sin cargar el componente destino.

---

## 20. API — Referencia de endpoints

### Autenticación `POST /api/v1/auth/*`

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `/register` | POST | Crear nueva cuenta | ❌ |
| `/login` | POST | Iniciar sesión | ❌ |
| `/refresh` | POST | Renovar access token con refresh token | ❌ |
| `/me` | GET | Obtener perfil del usuario autenticado | ✅ |
| `/me` | PUT | Actualizar perfil del usuario autenticado | ✅ |
| `/logout` | POST | Cerrar sesión (invalida refresh token) | ✅ |

### Vehículos `GET /api/v1/vehicles/*`

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `/` | GET | Listado paginado con filtros | ❌ |
| `/featured?count=6` | GET | Vehículos destacados para landing | ❌ |
| `/stats` | GET | Estadísticas globales de la plataforma | ❌ |
| `/makes` | GET | Lista de marcas disponibles | ❌ |
| `/:slug` | GET | Detalle de vehículo por slug | ❌ |
| `/:id/history` | GET | Historial de actividad del vehículo | ❌ |
| `/` | POST | Crear nuevo vehículo | ✅ |
| `/:id` | PUT | Actualizar vehículo (propietario) | ✅ |
| `/:id` | DELETE | Eliminar vehículo (propietario) | ✅ |
| `/:vehicleId/images` | POST | Subir imagen al vehículo | ✅ |
| `/:vehicleId/favorite` | POST | Toggle favorito | ✅ |

### Tramitación `GET /api/v1/compliance/*`

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `/requirements` | GET | Requisitos para par origen-destino | ❌ |
| `/estimate` | GET | Estimación de costes de importación | ❌ |
| `/templates` | GET | Plantillas de documentos | ❌ |
| `/processes/:id` | GET | Estado de un proceso específico | ✅ |
| `/processes` | POST | Iniciar nuevo proceso | ✅ |
| `/processes/:id/documents/:docId` | PUT | Subir/verificar documento | ✅ |

### Mensajería `GET /api/v1/messaging/*`

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `/conversations` | GET | Lista de conversaciones del usuario | ✅ |
| `/conversations/:id/messages` | GET | Mensajes de una conversación (paginado) | ✅ |
| `/send` | POST | Enviar mensaje | ✅ |

### Administración `GET /api/v1/admin/*`

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `/stats` | GET | Estadísticas globales de la plataforma | ✅ Admin |
| `/vehicles` | GET | Todos los vehículos con filtro de estado | ✅ Admin |
| `/vehicles/:id/approve` | POST | Aprobar vehículo (Reviewing → Active) | ✅ Admin |

### Otros

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `GET /api/v1/countries` | GET | Lista de países soportados | ❌ |
| `POST /api/v1/newsletter/subscribe` | POST | Suscribirse a la newsletter | ❌ |

---

## 21. Comunicación en tiempo real (SignalR)

**Hub URL**: `/hubs/chat`  
**Autenticación**: JWT Bearer token en la conexión.

### Métodos que el cliente invoca en el servidor

| Método | Parámetros | Descripción |
|---|---|---|
| `JoinConversation` | `conversationId: string` | Unirse a la sala de una conversación para recibir mensajes en tiempo real. |
| `LeaveConversation` | `conversationId: string` | Abandonar la sala al salir de la vista de conversación. |
| `SendMessage` | `recipientId, vehicleId, body` | Enviar un mensaje (alternativa al endpoint REST). |

### Eventos que el servidor envía al cliente

| Evento | Payload | Descripción |
|---|---|---|
| `ReceiveMessage` | `{ messageId, senderId, vehicleId, body, createdAt }` | Nuevo mensaje recibido en una conversación activa. |

### Ciclo de vida en el frontend

```
ConversationComponent.ngOnInit()
  ↓
MessagingService.startConnection(accessToken)
  ↓ SignalR conectado
JoinConversation(conversationId)
  ↓
[Escucha ReceiveMessage]
  → Si senderId ≠ currentUserId → recarga mensajes
  ↓
ConversationComponent.ngOnDestroy()
  → LeaveConversation(conversationId)
  → MessagingService.stopConnection()
```

---

*Documentación generada en Abril 2025 para el equipo de desarrollo y producto de Logistique Les Lions.*
