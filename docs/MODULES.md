# Módulos — Estado de Implementación

## Leyenda
- ✅ Implementado y funcional
- 🔨 En progreso (backlog pendiente de refinamiento)
- ⬜ Pendiente

---

## Módulo 1 — Landing Page & SEO ✅

**Componentes:**
- ✅ `HeroSearchComponent` — formulario con autocompletado de marcas y países
- ✅ `StatsCountersComponent` — contadores animados con IntersectionObserver
- ✅ `FeaturedVehiclesComponent` — grid con skeleton loading
- ✅ `HowItWorksComponent` — stepper animado con tabs comprador/vendedor
- ✅ `CountryMapComponent` — grid interactivo de países con panel de detalle
- ✅ `NewsletterComponent` — formulario con validación reactiva
- ✅ `CookieConsentComponent` — banner RGPD con preferencias granulares

**API:** `GET /featured`, `GET /stats`, `GET /makes`, `GET /countries/supported`

---

## Módulo 2 — Vehículos ✅

**Componentes:**
- ✅ `VehicleListComponent` — grid/list, filtros, sort, paginación
- ✅ `VehicleCardComponent` — card reutilizable con favorites
- ✅ `VehicleDetailComponent` — galería, specs, botón "Contactar vendedor"
- ✅ `VehicleFormComponent` — wizard 4 pasos para publicar
- ✅ `MyVehiclesComponent` — listado de anuncios propios (`/mis-vehiculos`)
- ✅ `FilterPanelComponent` — panel de filtros lateral

**API:** CRUD completo, imágenes (multipart), favoritos, historial, `sellerId` filter

**Storage:** LocalStorageService (local dev) → sustituible por R2/S3 en producción

---

## Módulo 3 — Tramitación Internacional ✅

**Componentes:**
- ✅ `ComplianceHomeComponent` — búsqueda de requisitos por par origen/destino
- ✅ `CostEstimatorComponent` — calculadora aranceles + IVA + homologación
- ✅ `DocumentChecklistComponent` — estado por documento con upload
- ✅ `ProcessTrackerComponent` — timeline con métricas de completitud

**Rutas de país implementadas:** ES↔DE, ES↔FR, US→ES, JP→ES, ES→MA, ES→GB

---

## Módulo 4 — Transacciones y Pagos ⬜

**Pendiente:**
- Integración Stripe (cards, SEPA, bank transfer)
- Escrow: retención hasta confirmación de entrega
- Facturación automática con IVA intra/extra-UE
- Informe fiscal para declaración de vendedor

---

## Módulo 5 — Mensajería ✅

- ✅ Conversaciones asociadas a vehículo (Buyer ↔ Seller)
- ✅ `InboxComponent` — listado de conversaciones con unread badge
- ✅ `ConversationComponent` — vista de mensajes con input
- ✅ `ChatHub` (SignalR) — tiempo real via WebSockets
- ✅ REST fallback para fiabilidad

**Pendiente:**
- 🔨 Traducción automática (DeepL API)
- 🔨 Plantillas de mensajes predefinidas
- 🔨 Web Push notifications

---

## Módulo 6 — Usuarios y Autenticación ✅

- ✅ Registro/login con email + JWT (access 15min + refresh 30d)
- ✅ BCrypt password hashing
- ✅ Roles: Buyer, Seller, Dealer, Admin, Moderator
- ✅ `LoginComponent` + `RegisterComponent`
- ✅ `ProfileComponent` — edición de datos personales y empresa
- ✅ `AuthGuard`, `AdminGuard`, `GuestGuard`
- ✅ `DealersComponent` — directorio de concesionarios (`/concesionarios`)

**Pendiente:**
- 🔨 OAuth (Google, Apple)
- 🔨 KYC — verificación de identidad
- 🔨 2FA con TOTP
- 🔨 Perfiles de concesionario con estadísticas públicas
- 🔨 Suscripciones Stripe para dealers

---

## Módulo 7 — Valoraciones e Inspecciones ⬜

---

## Módulo 8 — Transporte Internacional ⬜

---

## Módulo 9 — Finanzas y Financiación ⬜

---

## Módulo 10 — Panel de Administración ✅

- ✅ `AdminDashboardComponent` — KPIs (vehículos, usuarios, procesos, valor)
- ✅ Tabla de vehículos con filtro por estado y aprobación
- ✅ `AdminGuard` — acceso restringido a rol Admin
- ✅ Endpoints: `GET /stats`, `GET /vehicles`, `POST /vehicles/:id/approve`

**Pendiente:**
- 🔨 Gestión de usuarios (ban, verificación)
- 🔨 Moderación de contenido
- 🔨 Logs de auditoría

---

## Infraestructura Transversal

| Componente | Estado |
|---|---|
| Docker Compose (PG + Redis + pgAdmin + API + Frontend) | ✅ |
| init.sql — schemas + extensiones PostgreSQL | ✅ |
| Clean Architecture 4 capas | ✅ |
| AuditInterceptor (soft delete automático) | ✅ |
| ValidationBehavior + LoggingBehavior (MediatR) | ✅ |
| ExceptionHandlerMiddleware (RFC 7807) | ✅ |
| CorrelationId middleware | ✅ |
| Rate limiting (IP 100/min + Auth 10/5min) | ✅ |
| CORS configurado | ✅ |
| JWT auth (access 15min + refresh 30d) | ✅ |
| BCrypt password hashing | ✅ |
| Redis caché distribuido | ✅ |
| LocalStorageService (uploads locales dev) | ✅ |
| Serilog estructurado + rolling files | ✅ |
| OpenTelemetry | ✅ |
| OpenAPI / Scalar UI | ✅ |
| SignalR (ChatHub) | ✅ |
| Angular 19 standalone + signals | ✅ |
| Tailwind v4 CSS-first | ✅ |
| Angular Router con lazy loading | ✅ |
| Auth + Error interceptors | ✅ |
| AuthGuard / AdminGuard / GuestGuard | ✅ |
| PWA (service worker) | ✅ config |
| SSR (Angular Universal) | ✅ config |
| i18n ES/EN/FR/DE | ✅ archivos |
| Tests unitarios xUnit + FluentAssertions | ✅ base |
| EF Migrations | 🔨 ejecutar `dotnet ef migrations add InitialCreate` |
