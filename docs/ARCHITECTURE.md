# Arquitectura — Logistique Les Lions

## Visión General

Plataforma SaaS de compraventa internacional de vehículos con gestión documental transfronteriza completa. Diseñada para escalar horizontalmente desde el día 1 con separación estricta de responsabilidades.

```
┌─────────────────────────────────────────────────────────────────┐
│                     CLIENTES / NAVEGADORES                       │
│             Angular 19 SPA + PWA (Service Worker)               │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTPS / REST+JSON
                             │ WebSocket (SignalR — mensajería)
┌────────────────────────────▼────────────────────────────────────┐
│                    LOGISTIQUE LES LIONS API                      │
│              .NET 9 Minimal APIs — ASP.NET Core 9               │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  Auth/JWT    │  │  Rate Limit  │  │  CorrelationId MW    │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    MediatR Pipeline                       │   │
│  │  LoggingBehavior → ValidationBehavior → Handler          │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────┬──────────────────────────────────────┬───────────────────┘
       │ EF Core 9 + Npgsql                   │ StackExchange.Redis
┌──────▼──────────┐                  ┌────────▼────────────────────┐
│  PostgreSQL 16  │                  │  Redis 7 (Caché + Sessions)  │
│                 │                  │                              │
│  Schema: vehicles│                 │  TTL: featured=5min          │
│  Schema: users  │                  │  TTL: countries=1h           │
│  Schema: txns   │                  │  TTL: makes=24h              │
│  Schema: compl. │                  └─────────────────────────────┘
│  Schema: msg    │
│  Schema: reviews│
└─────────────────┘
```

## Decisiones Técnicas Clave

### 1. Clean Architecture en 4 capas

```
Domain → Application → Infrastructure → API
  ↑           ↑              ↑            ↑
Sin deps   Solo Domain   App + Domain  Todo + Infra
```

**Por qué:** Testabilidad total. Los Handlers de Application nunca importan de Infrastructure. Los tests pueden usar InMemory DB sin cambiar una línea de lógica de negocio.

### 2. CQRS con MediatR

Cada operación es un Command (escritura) o Query (lectura) con su Handler dedicado. Ventajas:
- Fácil de testear en aislamiento
- Pipeline de behaviors reutilizable (logging, validación, caché)
- Escalabilidad: las Queries pueden dirigirse a réplicas de lectura en el futuro

### 3. Result<T> en lugar de excepciones

```csharp
// ✓ Correcto: flujo de negocio con Result<T>
return Result<Guid>.Failure("Vehicle.NotFound");

// ✗ Incorrecto: nunca para lógica esperada
throw new NotFoundException("Vehicle not found");
```

Las excepciones solo se usan para errores de infraestructura inesperados. El ExceptionHandlerMiddleware global los captura y los convierte en ProblemDetails (RFC 7807).

### 4. Soft Delete universal

Todas las entidades heredan de `AuditableEntity` que tiene `DeletedAt`. El `AuditInterceptor` convierte automáticamente cualquier `DELETE` en un `UPDATE` con `deleted_at = NOW()`. Los `HasQueryFilter(e => !e.IsDeleted)` en las configuraciones de EF excluyen los registros borrados automáticamente.

### 5. PostgreSQL Schemas separados por módulo

Cada módulo tiene su propio schema (`vehicles`, `users`, `transactions`, etc.) para:
- Aislar dependencias entre módulos
- Facilitar extracción a microservicios en el futuro
- Aplicar Row Level Security (RLS) granularmente
- Gestionar migraciones independientes por módulo

### 6. Caché por capas con Redis

| Recurso | TTL | Motivo |
|---|---|---|
| `featured_vehicles_*` | 5 min | Cambia poco, carga la landing |
| `vehicle_stats` | 5 min | Solo contadores aproximados |
| `vehicle_makes_*` | 24 h | Datos casi estáticos |
| `supported_countries` | 1 h | Actualizaciones poco frecuentes |

### 7. Angular 19 con Signals

Usado `signal()` y `computed()` de Angular para estado reactivo sin necesidad de RxJS en componentes simples. Los Interceptors HTTP son funcionales (no clases), compatible con `provideHttpClient(withInterceptors([...]))`.

## Estructura de Módulos Backend

```
Application/Features/
├── Vehicles/
│   ├── Commands/      # CreateVehicle, UpdateVehicle, DeleteVehicle...
│   └── Queries/       # GetFeaturedVehicles, GetVehicles, GetVehicleBySlug...
├── Countries/
│   └── Queries/       # GetSupportedCountries
├── Compliance/        # [Próxima iteración]
├── Transactions/      # [Próxima iteración]
├── Users/             # [Próxima iteración]
└── Admin/             # [Próxima iteración]
```

## Seguridad

- **JWT**: Access token 15min + Refresh token 30 días. El AuthInterceptor en Angular renueva automáticamente.
- **Rate Limiting**: 100 req/min por IP general, 10 intentos/5min para auth.
- **CORS**: Origins explícitos. Nunca `*` en producción.
- **Headers de seguridad**: X-Content-Type-Options, X-Frame-Options, CSP.
- **Validación**: FluentValidation en cada Command/Query. Nunca confiar en datos del cliente.
- **Soft Delete**: Los datos nunca se borran físicamente, cumpiendo con auditoría completa.

## Infraestructura

```
Local:
  PostgreSQL (Docker) ← EF Core migrations → Schemas creados automáticamente
  Redis (Docker)      ← Caché distribuido
  Backend (dotnet run) en :5000
  Frontend (ng serve + proxy) en :4200 → /api → :5000

Producción:
  Neon (PostgreSQL serverless) ← Connection string en Render env vars
  Redis Cloud / Upstash        ← Redis serverless
  Render (Web Service)         ← Backend .NET 9
  Vercel                       ← Frontend Angular 19 (SSG/SSR)
```
