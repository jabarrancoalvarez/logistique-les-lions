# Logistique Les Lions — Documentación funcional y guía de usuario

> Plataforma SaaS de compraventa internacional de vehículos con gestión documental
> transfronteriza, calculadora de costes aduaneros, tramitación, marketplace de
> partners y mensajería en tiempo real.

---

## 1. Visión general

**Logistique Les Lions** conecta vendedores, compradores y profesionales del sector
del automóvil para simplificar la importación y exportación de vehículos entre la
Unión Europea, Reino Unido, Marruecos, Japón y EE.UU.

La plataforma cubre el ciclo completo:

1. **Publicación** del vehículo con descripción asistida por IA.
2. **Búsqueda y filtros** tipo Amazon (facetas dinámicas por marca, combustible,
   país, estado, etc.).
3. **Calculadora de aduanas** y simulador de tramitación.
4. **Tramitación documental** (homologación, transporte, COC, ITV) con seguimiento
   en tiempo real y gestión de incidencias.
5. **Marketplace de partners**: gestores, transportistas, inspectores,
   homologadores, aseguradoras y financieras.
6. **Mensajería interna** entre compradores, vendedores y partners.
7. **Notificaciones push** vía SignalR.
8. **Tracking público** del envío con código corto.

---

## 2. Roles de usuario

| Rol         | Capacidades principales |
|-------------|------------------------|
| **Buyer**     | Buscar, guardar favoritos, mensajería, comprar, hacer tracking |
| **Seller**    | Publicar anuncios, gestionar leads, descripción IA |
| **Dealer**    | Anuncios ilimitados, badge verificado, estadísticas, descripción IA |
| **Moderator** | Aprobar anuncios, resolver disputas, gestionar incidencias |
| **Admin**     | Acceso total: usuarios, partners, procesos, métricas |

Las políticas de autorización están configuradas en `Program.cs`:
`CanPublishVehicle`, `CanModerate`, `CanManageUsers`, `CanViewAdminPanel`,
`CanBuyVehicle`.

---

## 3. Arquitectura técnica

### 3.1 Backend (.NET 9 — Clean Architecture)

```
src/
├── LogistiqueLesLions.API/             # Endpoints minimal API, SignalR hubs
├── LogistiqueLesLions.Application/     # CQRS (MediatR), DTOs, interfaces
├── LogistiqueLesLions.Domain/          # Entidades, enums, eventos
└── LogistiqueLesLions.Infrastructure/  # EF Core, Identity, Anthropic, storage
```

- **Patrones:** Clean Architecture, CQRS con MediatR, `Result<T>` (sin
  excepciones para errores de negocio), Soft Delete global, Slugs únicos.
- **Persistencia:** PostgreSQL 16 con schemas separados por bounded context
  (`vehicles`, `compliance`, `messaging`, `marketplace`).
- **Auth:** ASP.NET Core Identity + JWT con refresh tokens.
- **IA:** Anthropic Claude API (Messages + Vision para OCR de documentos).
- **Tiempo real:** SignalR (`/hubs/chat`, `/hubs/notifications`).
- **Observabilidad:** Serilog + OpenTelemetry + Prometheus exporter en `/metrics`.
- **Webhooks:** firmados con HMAC-SHA256.

### 3.2 Frontend (Angular 19)

- Componentes **standalone** con `ChangeDetectionStrategy.OnPush`.
- Estado con **signals** y `computed`.
- Tailwind CSS **v3** (no actualizar a v4 — incompatible con esbuild de Angular).
- Lazy loading obligatorio en todas las rutas (`loadComponent` / `loadChildren`).
- HTTP interceptor que añade JWT y refresca tokens en 401.

### 3.3 Infraestructura

| Entorno     | Base de datos               | API                      | Frontend             |
|-------------|------------------------------|--------------------------|----------------------|
| **Local**   | PostgreSQL 16 (Docker)       | `dotnet run` (5000)      | `ng serve` (4200)    |
| **Producción** | Neon (PostgreSQL serverless) | Render                   | Vercel               |

---

## 4. Módulos funcionales

### 4.1 Vehículos

- **Listado**: paginación, filtros por marca, modelo, año, precio, país de origen,
  estado, combustible, transmisión, body type, ready-to-export, destacados.
- **Facetas dinámicas**: cuenta de coincidencias por categoría (estilo Amazon).
- **Detalle**: ficha completa, galería, historial, vehículos similares.
- **Crear/editar**: wizard multi-paso con descripción IA y OCR de documento.
- **Favoritos**: toggle por usuario.
- **Chat IA contextual**: el comprador hace preguntas sobre un vehículo concreto
  y Claude responde con la ficha como system prompt.

### 4.2 Tramitación / Compliance

- **Procesos** import/export con tracking code corto generado al alta.
- **Documentos** del proceso (COC, factura, ITV, transporte…).
- **Incidencias**: bloqueos abiertos por severidad, resolución por moderador.
- **Homologación**: requisitos por país y categoría de vehículo.
- **Aranceles**: tarifas por par origen-destino y código HS.
- **Calculadora de costes**:
  - `EstimateCost` — para un vehicleId existente.
  - `SimulateCost` — independiente, recibe precio + países.
- **Plantillas de documento** descargables por país y tipo.

### 4.3 Marketplace de partners

Tipos: **Gestor, Transport, Inspector, Homologator, Insurance, Financing**.

- Filtros por tipo y país (CSV de countries soportados).
- Ordenación: verificados → rating → nombre.
- Endpoint público para listar, endpoint admin para crear/editar.

### 4.4 Mensajería

- Conversaciones 1‑a‑1 entre comprador y vendedor.
- Mensajes con marca de leído/no leído.
- Hub SignalR `/hubs/chat` con eventos `ReceiveMessage`, `MessageRead`,
  `UserTyping`.

### 4.5 Notificaciones

- Centro persistente (`messaging.user_notifications`).
- Categorías: incident, process, message, system, marketplace.
- Push en tiempo real vía hub `/hubs/notifications` con fallback a persistencia
  (si SignalR falla, la notificación queda en BBDD).

### 4.6 Tracking público

- Endpoint **sin autenticación**: `/api/v1/public/tracking/{code}`.
- `/{code}/route` devuelve origen, destino, posición estimada y porcentaje
  completado, con interpolación lineal sobre centroides geográficos.

### 4.7 Newsletter

- Suscripción pública con doble opt-in opcional.

### 4.8 Exports

- Generación de informes admin (CSV/PDF) bajo `/api/v1/exports`.

---

## 5. Endpoints REST principales

Todos bajo el prefijo `/api/v1` con rate limiting `IpRateLimit` (100 req/min/IP).

### Vehículos
```
GET    /vehicles                     listado paginado + filtros
GET    /vehicles/facets              facetas para filtros
GET    /vehicles/{slug}              detalle
GET    /vehicles/{id}/history        historial
POST   /vehicles                     crear (auth)
PUT    /vehicles/{id}                actualizar (auth)
DELETE /vehicles/{id}                soft delete (auth)
POST   /vehicles/{id}/images         subir imagen multipart
POST   /vehicles/{id}/favorite       toggle favorito
POST   /vehicles/{id}/ai/description generar descripción IA
POST   /vehicles/ai/preview-description preview sin persistir
POST   /vehicles/{id}/ai/ask         chat IA contextual (público)
POST   /vehicles/ai/extract-document OCR de ficha técnica/COC
```

### Compliance
```
POST   /compliance/processes
GET    /compliance/processes
POST   /compliance/processes/{id}/incidents
POST   /compliance/incidents/{id}/resolve
GET    /compliance/incidents
POST   /compliance/simulate          calculadora independiente
POST   /compliance/estimate          calculadora con vehicleId
```

### Marketplace
```
GET    /marketplace/partners?type=&country=
POST   /marketplace/partners         (admin)
```

### Notificaciones
```
GET    /notifications
POST   /notifications/{id}/read
POST   /notifications/read-all
```

### Tracking público
```
GET    /public/tracking/{code}
GET    /public/tracking/{code}/route
```

### Auth
```
POST   /auth/register
POST   /auth/login
POST   /auth/refresh
POST   /auth/logout
```

---

## 6. Panel de administración

Ruta: `/admin/**` (solo rol Admin). Layout con sidebar persistente y secciones:

1. **Dashboard** — KPIs globales y últimos vehículos pendientes.
2. **Vehículos** — listado completo con estado de moderación.
3. **Procesos** — todos los procesos import/export en marcha.
4. **Incidencias** — bloqueos abiertos por severidad.
5. **Marketplace** — partners verificados/no verificados.
6. **Usuarios** — listado y gestión de cuentas.
7. **Notificaciones** — bandeja del admin actual.

Cada sección hace fetch directo a la API REST correspondiente y se renderiza
con OnPush + signals.

---

## 7. Cómo arrancar en local

### 7.1 Requisitos

- .NET 9 SDK
- Node 20+ y npm
- Docker Desktop (para PostgreSQL local)

### 7.2 Backend

```bash
cd backend

# 1. Levantar PostgreSQL
docker-compose up -d

# 2. Configurar appsettings.Development.json (no commiteado):
#    - ConnectionStrings:DefaultConnection
#    - Jwt:Key (32+ chars)
#    - Anthropic:ApiKey
#    - Seed:Enabled = true   (para llenar la BBDD con datos demo)

# 3. Aplicar migraciones (la API lo hace al arrancar) y arrancar
cd src/LogistiqueLesLions.API
dotnet run
```

La API queda en `http://localhost:5000`. Documentación interactiva en
`/scalar/v1`. Prometheus scrape en `/metrics`.

### 7.3 Frontend

```bash
cd frontend
npm install
npm run start
```

Frontend en `http://localhost:4200` con proxy `/api` → `http://localhost:5000`.

### 7.4 Datos demo

Si `Seed:Enabled=true`, al arrancar la API se ejecuta `DatabaseSeeder` que crea
de forma idempotente:

- 12 países, 12 marcas y ~60 modelos
- 10 usuarios (admin, moderador, dealers, sellers, buyers)
- 20 vehículos con imágenes
- 6 homologaciones, 6 aranceles, 8 plantillas de documento
- 8 procesos con documentos e incidencias
- 6 conversaciones con mensajes
- 30 notificaciones
- 12 partners de servicios
- 8 suscriptores de newsletter

**Credenciales de prueba** (todas con password `demo1234`):

| Rol       | Email                                 |
|-----------|----------------------------------------|
| Admin     | admin@logistique-les-lions.test        |
| Moderator | moderator@logistique-les-lions.test    |
| Dealer    | dealer1@logistique-les-lions.test      |
| Seller    | seller1@logistique-les-lions.test      |
| Buyer     | buyer1@logistique-les-lions.test       |

---

## 8. Despliegue producción

| Servicio | Plataforma | Variable clave |
|----------|------------|-----------------|
| API      | Render Web Service (Frankfurt) | `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Anthropic__ApiKey`, `ASPNETCORE_ENVIRONMENT=Production` |
| Frontend | Vercel | `apiUrl` del environment.production.ts apunta al dominio de Render |
| Base de datos | Neon (PostgreSQL serverless) | Connection string en variable de Render |

CI/CD: push a `main` dispara el build automático en Render y Vercel. Las
migraciones se aplican al arrancar la API.

**Importante en producción**: dejar `Seed:Enabled=false` (o sin definir) para no
inyectar datos demo en la BBDD real.

---

## 9. Convenciones obligatorias

### Backend

- ❌ No lanzar `Exception` para errores de negocio — usar `Result<T>`.
- ❌ No exponer entidades en la API — siempre DTOs.
- ❌ No referencias directas de `Application` a `Infrastructure`.
- ❌ No borrado físico — siempre soft delete.
- ✅ IDs como `Guid`, fechas como `DateTimeOffset`.

### Frontend

- ❌ No usar `NgModule`.
- ❌ No `Default` change detection — siempre `OnPush`.
- ❌ No `[class.text-X/Y]` con opacidades Tailwind — usar ternario completo.
- ❌ No actualizar a Tailwind v4.
- ✅ Mock data como constantes de módulo, nunca métodos de clase.
- ✅ Lazy loading obligatorio.

---

## 10. Soporte y contacto

- Repositorio: ver `README.md`
- Documentación API interactiva: `https://<host>/scalar/v1`
- Métricas: `https://<host>/metrics` (Prometheus)
- Healthcheck: `/health`, `/health/live`, `/health/ready`
