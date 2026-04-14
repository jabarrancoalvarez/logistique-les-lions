# Logistique Les Lions — Flujo de Pruebas

Guía paso a paso para arrancar la plataforma y validar **todas** las funcionalidades implementadas (backend .NET 9 + frontend Angular 19 + PostgreSQL). Sigue el orden — cada bloque depende del anterior.

---

## 0. Requisitos previos

| Herramienta | Versión mínima | Comprobar con |
|---|---|---|
| .NET SDK | 9.0 | `dotnet --version` |
| Node.js | 22+ | `node --version` |
| Angular CLI | 19+ | `ng version` |
| Docker Desktop | reciente | `docker --version` |
| PostgreSQL client (opcional) | 16 | `psql --version` |

```bash
dotnet --info
docker ps
```

---

## 1. Levantar la infraestructura (Docker)

Desde la raíz del proyecto:

```bash
cd P:\ClaudeCode\projects\logistique-les-lions
docker-compose up -d postgres redis pgadmin
```

**Qué validar:**
- `docker ps` muestra los 3 contenedores en estado `Up`.
- PostgreSQL accesible en `localhost:5432`.
- Redis accesible en `localhost:6379`.
- pgAdmin accesible en `http://localhost:5050` (credenciales en README).

---

## 2. Backend — Arrancar la API

```bash
cd backend
dotnet restore
dotnet run --project src/LogistiqueLesLions.API
```

**Qué validar:**
- `dotnet restore` termina sin errores de paquetes.
- Al arrancar, las **migraciones EF Core se aplican automáticamente** (log: `Applied migration ...`).
- La API queda escuchando en `http://localhost:5000`.
- `http://localhost:5000/health` devuelve `200 OK` con `status: Healthy`.
- `http://localhost:5000/scalar/v1` muestra la documentación de endpoints (Scalar).
- En consola no hay excepciones de conexión a PostgreSQL ni Redis.

---

## 3. Frontend — Arrancar Angular

En otra terminal:

```bash
cd frontend
npm install
npm start
```

**Qué validar:**
- `npm install` sin errores de peerDeps.
- Angular compila sin errores (Tailwind v3 activo — no subir a v4).
- App disponible en `http://localhost:4200`.
- El proxy a `/api` redirige correctamente a `http://localhost:5000` (sin errores CORS).
- Landing carga con la navbar, hero navy (`#0B1F3A`) y acentos dorados (`#C9A84C`).

---

## 4. Landing y navegación pública

Desde `http://localhost:4200/`:

1. La **LandingPageComponent** muestra hero + secciones.
2. Navegar desde la navbar a cada una de las rutas públicas:

| Ruta | Qué validar |
|---|---|
| `/vehiculos` | Listado público de vehículos activos (aunque esté vacío, debe cargar sin errores). |
| `/tramitacion` | Página de tramitación internacional. |
| `/tramitacion/calculadora` | Calculadora de costes/aranceles. |
| `/transporte` | Página de transporte. |
| `/financiacion` | Página de financiación. |
| `/precios` | Planes y precios. |
| `/inspectores` | Red de inspectores. |
| `/guias/importacion` | Guía de importación. |
| `/guias/exportacion` | Guía de exportación. |
| `/guias/homologacion` | Guía de homologación. |
| `/legal/aviso-legal` | Aviso legal. |
| `/legal/privacidad` | Privacidad. |
| `/legal/cookies` | Cookies. |
| `/legal/terminos` | Términos. |
| `/legal/rgpd` | RGPD. |

**Validar en todas:**
- No hay errores en la consola del navegador.
- Lazy loading funcional: en la pestaña Network de DevTools aparece un `chunk-*.js` nuevo por feature.
- Ningún bloque queda con `opacity: 0` (regla: no usar `.reveal` para contenido above-the-fold).

---

## 5. Autenticación — Registro y login

### 5.1 Registro de usuario `User`

1. Ir a `/auth/register`.
2. Rellenar: email `test.user@lll.dev`, password `Test1234!`, nombre, apellidos.
3. Enviar el formulario.

**Validar:**
- POST a `/api/auth/register` → **201 Created**.
- Se guarda en `localStorage`: `lll_access_token`, `lll_refresh_token`, `lll_user`.
- Redirección automática a `/dashboard`.
- La navbar muestra el avatar/nombre del usuario.

### 5.2 Logout y login

1. Hacer logout desde el menú del avatar.
2. **Validar:** las 3 claves de `localStorage` se borran, redirección a `/`.
3. Ir a `/auth/login`, loguear con las mismas credenciales.
4. **Validar:** POST a `/api/auth/login` → **200 OK** con `accessToken` + `refreshToken`. Se vuelve a `/dashboard`.

### 5.3 Refresh token

1. En DevTools → Application → Local Storage, editar `lll_access_token` para invalidarlo (cambiar un char).
2. Recargar cualquier ruta autenticada (ej. `/dashboard`).

**Validar:**
- Primera petición devuelve **401**.
- El interceptor llama automáticamente a `/api/auth/refresh`.
- Nuevo `lll_access_token` guardado, la petición original se reintenta y pasa.

### 5.4 Guards

| Acción | Esperado |
|---|---|
| Sin login ir a `/mis-vehiculos` | `authGuard` redirige a `/auth/login` |
| Sin login ir a `/vehiculos/nuevo` | redirección a `/auth/login` |
| Logueado ir a `/auth/login` | `guestGuard` redirige a ruta autenticada |
| Usuario rol `User` / `Buyer` ir a `/admin` | `adminGuard` redirige a `/` |

---

## 6. Vehículos — CRUD completo

### 6.1 Crear vehículo (`/vehiculos/nuevo`)

Autenticado como `User`, rellenar el `vehicle-form`:

| Campo | Valor |
|---|---|
| Título | `BMW 320d Test` |
| Marca / Modelo | BMW / Serie 3 (si aplica, seleccionar de dropdown) |
| Año | `2020` |
| Precio | `18500` |
| Kilómetros | `85000` |
| Combustible | Diésel |
| Cambio | Automático |
| País | España |
| Descripción | `Vehículo de prueba flujo QA` |
| Modelo opcional | dejar vacío a propósito |

**Validar:**
- Los campos opcionales vacíos se envían como `null` (no como string vacío que rompería el bind de `Guid?`).
- POST a `/api/vehicles` → **200/201** con `Result<Guid>.Success`.
- El `JsonStringEnumConverter` registrado en Minimal API acepta los enums como strings (no devuelve 400).
- Redirección al detalle del vehículo recién creado.
- El vehículo se publica **directamente en Active** (sin flujo de moderación todavía).

### 6.2 Subida de imágenes

En el formulario (o desde el detalle) subir 2 imágenes vía `image-upload`.

**Validar:**
- POST multipart a `/api/vehicles/{id}/images` → **200**.
- La URL de imagen devuelta se deriva del request en producción (no hardcodea `localhost`).
- La imagen se muestra en el detalle.

### 6.3 Slugs únicos

Crear otro vehículo con **exactamente el mismo título**.

**Validar:**
- El backend captura `DbUpdateException` por slug duplicado.
- Respuesta **409 Conflict** con `Result.Failure("Vehicle.SlugConflict")`.
- El frontend muestra el error en el formulario.

### 6.4 Listado público (`/vehiculos`)

**Validar:**
- GET `/api/vehicles` devuelve solo vehículos `IsDeleted=false` (query filter global).
- El vehículo creado aparece en la lista.
- Filtros (precio, marca, combustible, país) funcionan en el `filter-panel`.
- Animaciones de tarjetas se ejecutan con CSS `animate-fadeInUp` (nada queda invisible por `reveal`).

### 6.5 Detalle (`/vehiculos/:slug`)

1. Clic en una tarjeta del listado.
2. **Validar:**
   - GET `/api/vehicles/by-slug/{slug}` → `Result<VehicleDetailDto>`.
   - Galería de imágenes, datos, precio, descripción.
   - Botón "Contactar" visible si hay sesión.

### 6.6 Mis vehículos (`/vehiculos/mis` o equivalente en dashboard)

**Validar:**
- Lista solo los vehículos del usuario autenticado.
- Acciones editar / eliminar visibles.

### 6.7 Editar y soft delete

1. Editar el vehículo, cambiar precio a `17900`, guardar.
2. **Validar:** PUT `/api/vehicles/{id}` → **200**, nuevo precio reflejado en detalle.
3. Eliminar el vehículo.
4. **Validar:**
   - DELETE `/api/vehicles/{id}` → **204**.
   - El registro sigue en BBDD (`SELECT * FROM vehicles.vehicles WHERE id='...'` muestra `is_deleted=true`, `deleted_at` poblado).
   - Ya no aparece en `/vehiculos` (query filter global activo).

---

## 7. Área de usuario — `/dashboard`, `/mis-vehiculos` y `/perfil`

1. Acceder a `/dashboard` (`authGuard`).
2. **Validar:** `DashboardComponent` renderiza con:
   - Saludo al usuario por nombre.
   - Dos tarjetas de estadísticas: "Mis vehículos publicados" y "Vehículos en favoritos" con contadores reales (no `—`) tras unos ms.
   - Cuatro quick actions: Publicar vehículo, Mis vehículos, Favoritos, Mensajes.
3. Acceder a `/mis-vehiculos` → lista los vehículos del usuario autenticado.
4. Ir a `/perfil`, editar nombre/apellidos, guardar.
5. **Validar:** PUT al endpoint de usuarios → **204**, los nuevos datos persisten tras refresh.

---

## 8. Favoritos (`/favoritos`)

1. Desde el detalle de un vehículo, marcarlo como favorito (POST `/api/v1/vehicles/{id}/favorite?userId=...` → 200 con `{ isSaved: true }`).
2. Ir a `/favoritos` (`authGuard`).
3. **Validar:** `FavoritesComponent` lista el vehículo recién guardado. GET `/api/v1/vehicles/favorites?userId=...` → 200 con array de `VehicleListDto`.
4. Pulsar el botón de quitar (corazón rojo) en uno de los items.
5. **Validar:** el item desaparece de la lista sin recargar, `FavoritesCount` del vehículo se decrementa.

---

## 9. Mensajería y SignalR (`/mensajes`)

### 9.1 Conexión al hub

1. Con sesión abierta, ir a `/mensajes`.
2. Abrir DevTools → Network → WS.
3. **Validar:**
   - Conexión WebSocket a `/hubs/chat` establecida con el JWT en el handshake.
   - Estado `Connected`.

### 9.2 Enviar mensaje entre dos cuentas

1. Abrir una ventana incógnita, registrar un segundo usuario (`test.user2@lll.dev`).
2. Desde el detalle de un vehículo del usuario 1, iniciar conversación como usuario 2.
3. **Validar en ambos navegadores simultáneamente:**
   - El usuario 1 recibe el evento `ReceiveMessage` sin recargar.
   - Al leer el mensaje, el usuario 2 recibe `MessageRead`.
   - Al escribir, se emite `UserTyping` y el otro lo ve.
   - Reconexión automática: detener backend 3s, al rearrancar el hub se reconecta con exponential backoff.

---

## 10. Panel de administración (`/admin`)

Requiere un usuario con rol `Admin`. Promocionar manualmente en BBDD:

```sql
-- pgAdmin o psql
UPDATE identity.asp_net_user_roles SET role_id = (
  SELECT id FROM identity.asp_net_roles WHERE name = 'Admin'
) WHERE user_id = '<id-del-usuario>';
```

Loguear, ir a `/admin`.

**Validar:**
- `adminGuard` permite el acceso.
- `AdminLayoutComponent` se renderiza con el menú lateral.
- Las secciones (`admin/sections/`) cargan sin errores de lazy loading.
- Un usuario sin rol Admin es redirigido a `/`.

---

## 11. Módulos adicionales — Smoke tests

Recorrer rápidamente cada feature restante y validar que carga sin errores de consola:

| Módulo | Ruta | Validación mínima |
|---|---|---|
| Compliance / Tramitación | `/tramitacion`, `/tramitacion/calculadora` | Formularios visibles, cálculo responde |
| Transport | `/transporte` | Página carga con datos/mock |
| Financing | `/financiacion` | Simulador funcional |
| Pricing | `/precios` | Planes visibles, bindings `[class]` sin opacidades Tailwind `/60` inválidas |
| Inspectors | `/inspectores` | Listado carga |
| Guides | `/guias/importacion` etc. | Contenido renderiza por slug |
| Legal | `/legal/*` | Contenido renderiza por slug |
| Dealers | desde admin o listado | Perfil de dealer accesible |
| Tracking | donde aplique | Widget/estado de envíos |

---

## 12. Tests automatizados

### Backend

```bash
cd backend
dotnet test
```

**Validar:** todos los proyectos `*.Tests` pasan en verde. No mockear la BBDD — los tests de integración usan `WebApplicationFactory` + PostgreSQL en Docker.

### Frontend

```bash
cd frontend
npm test
```

**Validar:** suites Jasmine/Karma pasan sin errores.

---

## 13. Validación de patrones del proyecto

Revisar que no se han introducido regresiones sobre las reglas de `CLAUDE.md`:

- [ ] Los controllers devuelven `ActionResult<Result<T>>`, nunca lanzan excepciones para lógica de negocio.
- [ ] Los handlers viven en `Application/Features/<Entidad>/<Caso>/`.
- [ ] `Application` no referencia `Infrastructure` ni `API`.
- [ ] Soft delete: ninguna llamada a `_db.Remove()` en handlers.
- [ ] IDs `Guid`, fechas `DateTimeOffset`.
- [ ] Componentes Angular nuevos: `standalone: true` + `ChangeDetectionStrategy.OnPush`.
- [ ] Rutas nuevas usan `loadComponent` / `loadChildren` (lazy).
- [ ] Mock data como constantes de módulo, no como métodos de clase.
- [ ] Sin `[class.text-X/60]` en templates (regla Tailwind/Angular).
- [ ] Tailwind sigue en v3 (no subir a v4).

---

## 14. Checklist final

- [ ] Docker: postgres + redis + pgadmin en `Up`
- [ ] Backend arranca y `/health` = Healthy
- [ ] Migraciones EF Core aplicadas automáticamente
- [ ] Scalar `/scalar/v1` accesible
- [ ] Frontend arranca sin errores, proxy `/api` OK
- [ ] Landing + todas las rutas públicas cargan
- [ ] Registro + login + refresh token + logout funcionan
- [ ] Guards (`auth`, `admin`, `guest`) redirigen correctamente
- [ ] CRUD de vehículos: crear, subir imágenes, listar, detalle, editar, soft delete
- [ ] Slug duplicado → 409 Conflict
- [ ] Favoritos funciona
- [ ] Mensajería en tiempo real vía SignalR entre 2 usuarios
- [ ] Panel `/admin` accesible solo con rol Admin
- [ ] Smoke test de módulos compliance/transport/financing/pricing/inspectors/guides/legal
- [ ] `dotnet test` y `npm test` en verde
- [ ] Sin errores ni warnings rojos en consola del navegador

---

## 15. Troubleshooting

| Síntoma | Causa probable | Solución |
|---|---|---|
| `400 Bad Request` al crear vehículo | Enum enviado como string sin converter | Verificar `JsonStringEnumConverter` registrado en Minimal API (`Program.cs`) |
| `Guid?` rompe el bind al crear vehículo | Campo opcional enviado como `""` | El form debe enviar `null` para opcionales vacíos |
| URL de imagen apunta a `localhost` en prod | `BaseUrl` no configurada | Se deriva del request automáticamente — comprobar middleware de storage |
| CORS bloqueado en frontend | Origin no está en `Cors__AllowedOrigins` | Añadir URL del frontend a la config del backend |
| `Failed to connect to hub /hubs/chat` | JWT no enviado en handshake | Revisar el `accessTokenFactory` del cliente SignalR |
| Tailwind no aplica estilos nuevos | Build caché de esbuild | `npm run start` con `--force` o borrar `.angular/cache` |
| Landing con secciones invisibles | Uso de `.reveal` + IntersectionObserver no dispara | Migrar a `animate-fadeInUp` con `animation-fill-mode: forwards` |
| Migraciones no se aplican | Entorno != Development | En Development se aplican automáticamente; en Prod ejecutar `dotnet ef database update` |
| `409 Conflict` al editar sin cambiar título | Constraint de slug mal interpretada | Verificar que solo se regenera el slug si cambia el título |

---

**Fin del flujo de pruebas.**
Si todo el checklist de la sección 14 está marcado, la plataforma Logistique Les Lions está validada end-to-end según `CLAUDE.md` y `docs/ARCHITECTURE.md`.
