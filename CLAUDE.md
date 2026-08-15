# CLAUDE.md — Yoon u Auto (antes Logistique Les Lions)

> Instrucciones específicas del proyecto para Claude Code.
> Escrito desde la perspectiva de un arquitecto senior .NET + Angular.
> Prevalece sobre el CLAUDE.md global en caso de conflicto.

---

## ⚠️ Migración en curso hacia la especificación Yoon u Auto

La aplicación se está adaptando a **`Yoon u Auto DOC APP.md`** (spec funcional v1.0, MVP).
Ese documento es la **fuente de verdad funcional**; este CLAUDE.md describe todavía en
parte el producto anterior.

- Hoja de ruta y estado por partes: [`docs/MIGRACION-YOON-U-AUTO.md`](docs/MIGRACION-YOON-U-AUTO.md)
- Módulos del producto anterior pendientes de decisión: [`docs/MODULOS-LEGACY.md`](docs/MODULOS-LEGACY.md)
- Deuda técnica detectada, para resolver al cerrar la migración: [`docs/PENDIENTES-TECNICOS.md`](docs/PENDIENTES-TECNICOS.md)
- **Comprobaciones obligatorias al aplicar las migraciones y desplegar**: [`docs/VERIFICACION-AL-MIGRAR.md`](docs/VERIFICACION-AL-MIGRAR.md)

Reglas ya vigentes tras la Parte 1:

| Regla | Detalle |
|---|---|
| **Idioma** | Todo texto visible al público, en **francés**, escrito directamente en las plantillas |
| **Moneda** | **FCFA (XOF)** únicamente. Formatear con el pipe `fcfa` (`8.900.000 FCFA`). No usar `CurrencyPipe` ni multi-divisa |
| **Geografía** | **Senegal**: 14 regiones y sus ciudades en `shared/data/senegal-geo.ts` |
| **Locale** | `LOCALE_ID: 'fr'` y `DEFAULT_CURRENCY_CODE: 'XOF'` en `app.config.ts` |
| **Namespaces** | Se mantiene `LogistiqueLesLions.*` — el renombrado se hará al final |
| **IA** | ❌ No generar ni modificar descripciones de anuncios con IA (lo prohíbe el doc) |
| **Identidad** | El **teléfono** (`+221XXXXXXXXX`) es el identificador de la cuenta. El correo es opcional, solo para notificaciones |
| **Roles** | Solo `User` y `Admin`. Todo gratuito e ilimitado: publicar/comprar/negociar solo exige estar autenticado |
| **Particulier/Professionnel** | Campo informativo del perfil. ❌ Nunca usarlo para autorizar ni para limitar anuncios |
| **Estados del anuncio** | `Brouillon · Actif · EnPause · Reserve · Vendu · Archive` |
| **Enums en francés** | `FuelType` (Diesel/Essence/Hybride/…), `TransmissionType` (Manuel/Automatique), `BodyType` (Citadine/Berline/Break/Suv/…), `CustomsStatus` (Dedouane/NonDedouane/Passavant) |
| **Referencia pública** | Todo anuncio tiene `PublicReference` (`YU12345`) desde una secuencia PostgreSQL. Usarla en chat, ofertas, contratos y soporte en lugar del UUID |
| **Precio** | Cada cambio de precio añade una fila a `VehiclePriceHistory`. Nunca modificar ni borrar ese histórico |
| **Equipamiento** | Catálogo en BD (`vehicle_equipments`) + tabla de enlace. ❌ No volver a listas JSONB |
| **Indicador de precio** | Estadístico, sin IA. Parámetros en `price_indicator_settings` (BD), ❌ nunca constantes en código. Si no hay comparables suficientes, **no se muestra nada** |
| **Seeds `HasData`** | Usar timestamps fijos, ❌ nunca `DateTimeOffset.UtcNow`: genera `UpdateData` espurios en cada migración |
| **Alerta ≠ Notificación** | La **alerta** es una regla del usuario (favorito, búsqueda guardada). La **notificación** es el evento que el sistema genera al cumplirse. Nunca modelar las alertas como entidad propia |
| **Notificaciones** | Persistir dentro de la transacción de negocio y empujar por SignalR **después** de `SaveChanges`, con `INotificationPusher`. Categorías en `NotificationCategories` |
| **Identidad en endpoints** | El usuario sale **siempre** del JWT. ❌ Nunca aceptar `userId` por query string ni por el cuerpo |
| **Negociación** | `Negotiation` es el agregado raíz de la Etapa 2 (antes `Conversation`). Chat, ofertas, inspección y contrato **cuelgan de ella**. ❌ No crear módulos sueltos de mensajes/ofertas/contratos |
| **Cronología** | `NegotiationEvent` es append-only: cada funcionalidad de la Etapa 2 añade sus hitos al mismo hilo. Se ordena por `Sequence`, no por `CreatedAt` |
| **Contrato** | Los datos del vehículo y de las partes se **congelan** al crearlo: nunca leerlos por referencia del anuncio. Redacta una parte y valida **siempre la otra** (`Contract.ValidatorId`) |
| **Venta verificada** | Validar el contrato es lo único que la produce: anuncio → `Vendu`, negociación → `Terminee`, `Seller.VerifiedSalesCount++`. ❌ No marcar ventas verificadas desde ningún otro sitio |
| **Mon Garage** | `GarageVehicle` es una entidad propia, **nunca** un `Vehicle` con bandera. Es privado: toda consulta filtra por `UserId`. El kilometraje solo avanza. Lo comprado en la plataforma entra una sola vez, vía `SourceContractId` |
| **Vendre ce véhicule** | Crea un anuncio en `Brouillon` desde Mon Garage. ❌ Nunca publicar automáticamente ni heredar precio, estado aduanero o descripción: son lo que el usuario debe revisar |
| **Transparence** | Nada del historial privado se publica sin marcarlo expresamente. Compartir una intervención **no** comparte su factura: son dos casillas |
| **Administración** | Toda medida que afecte a lo que el usuario ve **exige motivo** y deja fila en `admin_actions` (append-only). ❌ El administrador nunca edita información comercial de un anuncio: pide la corrección. Ocultar usa `AdminHiddenAt`, nunca el estado `EnPause` |
| **Privacidad en el backoffice** | El administrador ve la **estructura** de una negociación, nunca el contenido. Leer los mensajes exige motivo y **queda registrado en la misma operación**. ❌ Nunca añadir los mensajes a un DTO de listado o ficha |
| **Contratos y admin** | El administrador solo puede **invalidar** un contrato, con motivo. ❌ Nunca validar en nombre de las partes |
| **Estadísticas** | Los precios se leen por su **mediana** (la media solo como contraste). La demanda cuenta **personas distintas**, no búsquedas. ❌ Nunca inventar un valor central cuando no hay datos: se devuelve `null` y la pantalla muestra «—» |
| **Complétude** | Mide lo completo y actualizado que está el **historial digital**. ❌ Nunca presentarla como diagnóstico mecánico ni certificación del estado del vehículo: el aviso debe estar visible en la propia pantalla |
| **Archivos privados** | La documentación de Mon Garage usa `IStorageService.UploadPrivateAsync` (fuera de `uploads/`, que se sirve estático) y se descarga por endpoint autenticado. ❌ Nunca exponer `StorageKey` en un DTO ni subir documentos con `UploadAsync` |
| **QR del contrato** | `Contract.VerificationCode` se genera al validar, es aleatorio (nunca derivado de `PublicReference`) y abre la página pública `/verification/:code`. ❌ Esa página no expone documentos de identidad, direcciones ni teléfonos |

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

### Identidad de marca — Yoon U Auto (azul & blanco)

La marca es **Yoon U Auto — Services Automobiles au Sénégal**. Paleta derivada del logo
(azul profundo + azul brillante + plata/blanco). Definida en `tailwind.config.js` y
como custom properties en `src/styles.css`.

| Token | Hex | Uso |
|---|---|---|
| `navy` | `#0A2E4D` | Azul profundo: navbar, footer, secciones oscuras, texto principal |
| `navy-light` | `#1F588F` | Degradados y hover sobre azul |
| `navy-dark` | `#061F36` | Fondos más profundos |
| `azure` | `#22A7D2` | Acento del logo: CTAs, enlaces, iconos (**sobre fondo oscuro**) |
| `azure-light` | `#7FD3EC` | Acento sobre azul profundo (navbar/footer) |
| `azure-dark` | `#157FA8` | Acento **sobre fondo claro** (4.5:1 sobre blanco) |
| `frost` | `#F4F8FB` | Fondo de página |
| `frost-dark` | `#E3EDF5` | Secciones alternas |
| `silver` / `steel` | `#C7D5E0` / `#5B7185` | Bordes y texto secundario |

- Tipografías: **Montserrat** (headings) + **Inter** (body).
- Clases de componente: `.btn-primary`, `.btn-azure`, `.btn-outline`, `.btn-outline-azure`,
  `.card`, `.badge-azure`, `.divider-azure`, `.surface-brand`, `.logo-chip`.
- **Contraste**: `text-azure` dentro de `.card` / `.bg-white` se oscurece automáticamente
  a `azure-dark` (regla en `styles.css`). Sobre fondo azul usar `text-azure` o `text-azure-light`.
- Logo: `assets/logo.png` (fondo blanco, va dentro de `.logo-chip`),
  `assets/logo-transparent.png`, `assets/logo-mark.png` (solo el símbolo de carretera),
  `assets/icons/*` y `public/favicon.ico` generados desde el mismo original.
- ❌ No quedan tokens `gold` ni `ivory`: se renombraron a `azure` y `frost` en todo el frontend.

### Tailwind CSS — AVISO CRÍTICO: Solo v3

```
⚠️  Este proyecto usa Tailwind CSS v3. NO actualizar a v4.
```

**Por qué**: Angular 19 usa esbuild como builder por defecto. Esbuild resuelve `@import "tailwindcss"` antes de que PostCSS procese el archivo. Tailwind v4 requiere ese import; v3 usa `@tailwind base/components/utilities` que funciona correctamente con el pipeline de PostCSS de Angular.

### Tailwind — Restricción en bindings de clase Angular

```html
<!-- ❌ INVÁLIDO: Angular no admite "/" en nombres de clase en [class.X] -->
<p [class.text-frost/60]="plan.highlighted">...</p>

<!-- ✅ Correcto: ternario con string completo -->
<p [class]="plan.highlighted ? 'text-frost opacity-60' : 'text-navy opacity-50'">...</p>

<!-- ✅ Alternativa: método helper en el componente -->
<p [class]="labelClass(plan)">...</p>
```

**Por qué**: Angular parsea el nombre de la clase en `[class.X]` como identificador. El `/` que Tailwind usa para modificadores de opacidad (`text-frost/60`) es inválido como identificador Angular.

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
/dashboard                → DashboardComponent (authGuard) — panel personal con stats y accesos
/mis-vehiculos            → MyVehiclesComponent (authGuard)
/favoritos                → FavoritesComponent (authGuard)
/admin/**                 → AdminModule (adminGuard)
/mensajes                 → MessagesComponent (authGuard)
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

| Actor | Acceso |
|---|---|
| Visitante (sin cuenta) | Consultar, buscar, filtrar, ordenar y compartir anuncios. No ve el teléfono del vendedor |
| `User` | Todo lo anterior + comprar, vender, publicar (sin límite), chatear, ofertar, contratos, Mon Garage. **Gratuito** |
| `Admin` | Todo el sistema + backoffice |

> Ya no existen `Dealer`, `Seller`, `Buyer` ni `Moderator`. `Particulier`/`Professionnel`
> es un campo del perfil (`AccountType`), no un rol.

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
