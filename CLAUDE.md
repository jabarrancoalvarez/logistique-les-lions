# CLAUDE.md — Logistique Les Lions

> Instrucciones específicas del proyecto para Claude Code.
> Escrito desde la perspectiva de un arquitecto senior .NET + Angular.
> Prevalece sobre el CLAUDE.md global en caso de conflicto.

---

## Stack del Proyecto

| Capa | Tecnología | Versión |
|---|---|---|
| Backend | ASP.NET Core + Clean Architecture | .NET 9 |
| ORM | Entity Framework Core | 9.x |
| Base de datos | PostgreSQL (local Docker / Neon en prod) | 16 |
| Auth | ASP.NET Core Identity + JWT + Refresh Tokens | — |
| Mensajería | MediatR (CQRS) | 12.x |
| Frontend | Angular standalone components | 19 |
| CSS | Tailwind CSS | **v3** (ver aviso crítico abajo) |
| Tiempo real | SignalR hub en `/hubs/chat` | — |
| Despliegue | Render (API) + Vercel (frontend) + Neon (DB) | — |

---

## Arquitectura Backend — Clean Architecture

### Estructura de proyectos

```
src/
├── LogistiqueLesLions.API/            # Entry point, controllers, Program.cs
├── LogistiqueLesLions.Application/    # Commands, Queries, DTOs, Interfaces
├── LogistiqueLesLions.Domain/         # Entities, Enums, Domain Events
└── LogistiqueLesLions.Infrastructure/ # DbContext, Migrations, Identity, externos
```

### Regla de dependencias (NUNCA invertir)

```
API → Application → Domain
Infrastructure → Application → Domain
```

- `Domain` no referencia ningún otro proyecto
- `Application` no referencia `Infrastructure` ni `API`
- `Infrastructure` implementa las interfaces definidas en `Application`

---

## Patrones Obligatorios Backend

### Result<T> — Error handling sin excepciones

```csharp
// ✅ Correcto: errores de negocio como Result
public async Task<Result<Guid>> Handle(CreateVehicleCommand cmd, CancellationToken ct)
{
    if (await _db.Vehicles.AnyAsync(v => v.Slug == cmd.Slug, ct))
        return Result<Guid>.Failure("Vehicle.SlugAlreadyExists");

    var vehicle = Vehicle.Create(cmd.Title, cmd.Slug, cmd.Price);
    _db.Vehicles.Add(vehicle);
    await _db.SaveChangesAsync(ct);
    return Result<Guid>.Success(vehicle.Id);
}

// ✅ Correcto: controller siempre ActionResult<Result<T>>
[HttpPost]
public async Task<ActionResult<Result<Guid>>> Create(CreateVehicleRequest req)
{
    var result = await _mediator.Send(req.ToCommand());
    return result.IsSuccess ? Ok(result) : BadRequest(result);
}

// ❌ Incorrecto: no lanzar excepciones para lógica de negocio
throw new Exception("Slug ya existe");
```

### CQRS con MediatR

```csharp
// Comando: modifica estado, devuelve Result<T>
public record CreateVehicleCommand(string Title, string Slug, decimal Price, ...)
    : IRequest<Result<Guid>>;

// Query: solo lectura, devuelve Result<T>
public record GetVehicleBySlugQuery(string Slug)
    : IRequest<Result<VehicleDetailDto>>;

// Handler siempre en su propia carpeta:
// Application/Features/Vehicles/CreateVehicle/CreateVehicleCommandHandler.cs
```

### IIdentityService — Desacoplar Identity de Application

```csharp
// Interfaz en Application/Interfaces/ — sin referencia a Identity
public interface IIdentityService
{
    Task<Result<Guid>> CreateUserAsync(string email, string password, string role);
    Task<Result<TokenPairDto>> LoginAsync(string email, string password);
    Task<Result<TokenPairDto>> RefreshAsync(string refreshToken);
}

// Implementación en Infrastructure/Identity/IdentityService.cs
```

### Soft Delete

- Todas las entidades principales implementan `IHasSoftDelete` (`IsDeleted`, `DeletedAt`)
- El `DbContext` aplica `HasQueryFilter(e => !e.IsDeleted)` globalmente
- **Nunca** llamar a `_db.Remove()` — siempre `entity.SoftDelete()`
- Para consultas admin que necesiten ver eliminados: `.IgnoreQueryFilters()`

### Slugs únicos

- Los recursos con URLs amigables tienen campo `Slug` con índice `UNIQUE`
- En el handler: capturar `DbUpdateException` → comprobar si la constraint es de slug → `Result.Failure("X.SlugConflict")`
- En el controller: devolver `409 Conflict` si el error es de slug

### Entities — Convenciones

```csharp
// ✅ IDs como Guid (never int para entidades de dominio)
public Guid Id { get; private set; } = Guid.NewGuid();

// ✅ Constructor privado para EF, factory method público
private Vehicle() { }
public static Vehicle Create(string title, string slug, decimal price) { ... }

// ✅ Propiedades con setters privados
public string Title { get; private set; } = string.Empty;

// ✅ Fechas como DateTimeOffset (no DateTime)
public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
```

---

## Patrones Obligatorios Frontend (Angular 19)

### Componentes siempre standalone

```typescript
@Component({
  selector: 'lll-vehicle-card',
  standalone: true,                          // ✅ siempre
  changeDetection: ChangeDetectionStrategy.OnPush,  // ✅ siempre
  imports: [RouterLink, CommonModule, ...],
  templateUrl: './vehicle-card.component.html'
})
```

### Signals — Estado reactivo

```typescript
// ✅ Estado con signal
readonly vehicles = signal<Vehicle[]>([]);
readonly isLoading = signal(false);

// ✅ Computed derivado
readonly hasVehicles = computed(() => this.vehicles().length > 0);

// ✅ Actualizar en subscribe
this.service.getAll().subscribe({
  next: list => this.vehicles.set(list),
  error: () => this.hasError.set(true)
});
```

### Mock data — SIEMPRE constante a nivel de módulo

```typescript
// ✅ Correcto: constante fuera de la clase
const MOCK_VEHICLES: Vehicle[] = [
  { id: '1', title: 'BMW 320d', ... }
];

@Component({ ... })
export class VehiclesComponent {
  readonly vehicles = signal<Vehicle[]>(MOCK_VEHICLES);  // ✅ seguro
}

// ❌ Incorrecto: llamar this.method() en inicializador de propiedad
export class VehiclesComponent {
  readonly vehicles = signal<Vehicle[]>(this.getMockVehicles());  // ❌ puede fallar
}
```

**Por qué**: Angular puede compilar la clase antes de que el prototipo esté completamente inicializado. Las constantes de módulo siempre están disponibles.

### Lazy loading — obligatorio en todas las rutas

```typescript
// ✅ Correcto
{
  path: 'vehiculos',
  loadChildren: () => import('./features/vehicles/vehicles.routes')
    .then(m => m.VEHICLES_ROUTES)
},
{
  path: 'vehiculos/:slug',
  loadComponent: () => import('./features/vehicles/vehicle-detail/vehicle-detail.component')
    .then(m => m.VehicleDetailComponent)
}

// ❌ Incorrecto: importaciones directas en app.routes.ts
import { VehicleDetailComponent } from './features/vehicles/...';
```

### Animaciones — NO usar el patrón `reveal` con IntersectionObserver para contenido crítico

```html
<!-- ✅ Correcto: animación CSS que siempre se ejecuta -->
<div class="animate-fadeInUp"
     [style.animation-delay]="(i * 0.08) + 's'"
     style="opacity:0;animation-fill-mode:forwards">
  <lll-vehicle-card [vehicle]="vehicle" />
</div>

<!-- ❌ Problemático: reveal depende del IntersectionObserver del padre -->
<div class="reveal">...</div>
```

**Por qué**: `reveal` usa `opacity: 0` inicial + `IntersectionObserver` para añadir `.visible`. Si el observer no dispara (timing, threshold, viewport), el contenido permanece invisible. Las animaciones CSS con `animation-fill-mode: forwards` siempre se ejecutan.

### Tailwind CSS — AVISO CRÍTICO: Solo v3

```
⚠️  Este proyecto usa Tailwind CSS v3. NO actualizar a v4.
```

**Por qué**: Angular 19 usa esbuild como builder por defecto. Esbuild resuelve `@import "tailwindcss"` antes de que PostCSS procese el archivo. Tailwind v4 requiere ese import; v3 usa `@tailwind base/components/utilities` que funciona correctamente con el pipeline de PostCSS de Angular.

### Tailwind — Restricción en bindings de clase Angular

```html
<!-- ❌ INVÁLIDO: Angular no admite "/" en nombres de clase en [class.X] -->
<p [class.text-ivory/60]="plan.highlighted">...</p>

<!-- ✅ Correcto: ternario con string completo -->
<p [class]="plan.highlighted ? 'text-ivory opacity-60' : 'text-navy opacity-50'">...</p>

<!-- ✅ Alternativa: método helper en el componente -->
<p [class]="labelClass(plan)">...</p>
```

**Por qué**: Angular parsea el nombre de la clase en `[class.X]` como identificador. El `/` que Tailwind usa para modificadores de opacidad (`text-ivory/60`) es inválido como identificador Angular.

---

## Estructura de Rutas Frontend

```
/                         → LandingPageComponent
/auth/login               → LoginComponent
/auth/register            → RegisterComponent
/vehiculos                → VehicleListComponent
/vehiculos/:slug          → VehicleDetailComponent
/vehiculos/nuevo          → CreateVehicleComponent (authGuard)
/tramitacion              → TramitacionComponent
/tramitacion/calculadora  → CalculadoraComponent
/transporte               → TransportPageComponent
/financiacion             → FinancingPageComponent
/precios                  → PricingPageComponent
/inspectores              → InspectorsPageComponent
/guias/importacion        → GuidePageComponent (slug: importacion)
/guias/exportacion        → GuidePageComponent (slug: exportacion)
/guias/homologacion       → GuidePageComponent (slug: homologacion)
/legal/aviso-legal        → LegalPageComponent (slug: aviso-legal)
/legal/privacidad         → LegalPageComponent (slug: privacidad)
/legal/cookies            → LegalPageComponent (slug: cookies)
/legal/terminos           → LegalPageComponent (slug: terminos)
/legal/rgpd               → LegalPageComponent (slug: rgpd)
/dashboard                → DashboardComponent (authGuard)
/admin/**                 → AdminModule (adminGuard)
/mensajes                 → MessagesComponent (authGuard)
/favoritos                → FavoritesComponent (authGuard)
/perfil                   → ProfileComponent (authGuard)
```

### Guards disponibles

- `authGuard` — redirige a `/auth/login` si no autenticado
- `adminGuard` — redirige a `/` si no tiene rol Admin
- `guestGuard` — redirige a `/dashboard` si ya autenticado (para login/register)

---

## Autenticación — Flujo JWT + Refresh Token

### Storage

```typescript
// localStorage keys
'lll_access_token'   // JWT de corta duración (15 min)
'lll_refresh_token'  // Opaque token de larga duración (7 días)
'lll_user'           // UserDto serializado como JSON
```

### Interceptor HTTP

```typescript
// El interceptor añade el access token a todas las peticiones a /api
// Si recibe 401, llama a /api/auth/refresh con el refresh token
// Si el refresh también falla, hace logout y redirige a login
```

### Roles

| Rol | Acceso |
|---|---|
| `Admin` | Todo el sistema + panel admin |
| `Dealer` | Anuncios ilimitados, estadísticas, badge verificado |
| `User` | Hasta 3 anuncios, búsqueda, favoritos, mensajes |

---

## SignalR — Chat en tiempo real

- Hub en `/hubs/chat`
- El cliente se conecta tras autenticarse con JWT
- Eventos del servidor: `ReceiveMessage`, `MessageRead`, `UserTyping`
- El cliente emite: `SendMessage`, `MarkAsRead`, `StartTyping`
- Reconexión automática con exponential backoff

---

## Convenciones de Código

### Nomenclatura

```
Componentes:  lll-feature-name        (prefijo lll-)
Servicios:    FeatureService          (en core/services/ si son singleton)
Commands:     CreateVehicleCommand    (verbo + entidad + Command)
Queries:      GetVehicleBySlugQuery   (Get + entidad + By + campo + Query)
DTOs:         VehicleDetailDto        (entidad + Detail/List/Summary + Dto)
Interfaces:   IVehicleRepository      (I + nombre)
```

### Archivos

```
vehicle-card.component.ts         (kebab-case)
vehicle-card.component.html
vehicle-card.component.spec.ts    (tests junto al componente)
CreateVehicleCommandHandler.cs    (PascalCase para C#)
```

### Tests

- **Angular**: Jasmine + Karma. Tests en `*.spec.ts` junto al componente.
- **.NET**: xUnit. Tests en proyecto separado `*.Tests/`.
- **No mockear la base de datos** en tests de integración — usar `WebApplicationFactory` con DB en memoria o PostgreSQL en Docker.

---

## Lo que NO se debe hacer

### Backend

- ❌ No lanzar `Exception` para errores de negocio — usar `Result<T>`
- ❌ No exponer entidades de dominio en la API — siempre DTOs
- ❌ No poner lógica de negocio en controllers — solo en handlers
- ❌ No referencias directas de `Application` a `Infrastructure`
- ❌ No hardcodear connection strings, API keys ni secrets en código
- ❌ No borrar físicamente registros — siempre soft delete
- ❌ No usar `int` como ID en entidades de dominio — siempre `Guid`

### Frontend

- ❌ No importar componentes directamente en `app.routes.ts` (rompe lazy loading)
- ❌ No usar `class="reveal"` para contenido crítico above-the-fold
- ❌ No inicializar signals con `this.method()` en property initializers
- ❌ No usar `[class.text-X/Y]` con opacidades Tailwind — usar ternario completo
- ❌ No actualizar a Tailwind v4 (incompatible con esbuild de Angular 19)
- ❌ No usar `NgModule` — todos los componentes son standalone
- ❌ No usar `Default` change detection — siempre `OnPush`

---

## Variables de Entorno

### Local (no commitear)

```bash
# appsettings.Development.json (en .gitignore)
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;..." },
  "Jwt": { "Key": "...", "Issuer": "...", "Audience": "..." },
  "Anthropic": { "ApiKey": "sk-ant-..." }
}
```

### Producción (Render env vars)

```
ConnectionStrings__DefaultConnection  → Neon connection string
Jwt__Key                              → Secret de 32+ chars
Anthropic__ApiKey                     → API key de Anthropic
Frontend__Url                         → URL de Vercel
ASPNETCORE_ENVIRONMENT                → Production
```

---

## Comandos Frecuentes

```bash
# Backend — arrancar en local
cd src/LogistiqueLesLions.API
dotnet run

# Backend — nueva migración
dotnet ef migrations add NombreDescriptivo --project ../LogistiqueLesLions.Infrastructure

# Backend — aplicar migraciones
dotnet ef database update --project ../LogistiqueLesLions.Infrastructure

# Frontend — arrancar en local (con proxy → localhost:5000)
cd frontend
npm run start

# Frontend — build de producción
npm run build

# Docker — levantar PostgreSQL
docker-compose up -d
```

---

## Checklist antes de commitear

- [ ] Sin secrets en código (connection strings, API keys, tokens)
- [ ] Los controllers devuelven `ActionResult<Result<T>>`
- [ ] Los handlers no lanzan excepciones para errores de negocio
- [ ] Los nuevos componentes Angular son `standalone: true` + `OnPush`
- [ ] Las nuevas rutas usan `loadComponent` / `loadChildren`
- [ ] No hay `[class.X/Y]` con opacidades Tailwind en templates
- [ ] Los mock data son constantes de módulo, no métodos de clase
- [ ] `appsettings.Development.json` en `.gitignore` y no commiteado

---

## Protocolo de Memoria

- Al **INICIO** de cada sesión: busca en memory el contexto de este proyecto
- Al **FINAL** de cada tarea: guarda en memory el estado actual y decisiones tomadas
