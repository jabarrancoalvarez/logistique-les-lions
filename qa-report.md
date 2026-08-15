# QA Report — Logistique Les Lions (E2E Producción)

**Fecha:** 2026-04-14
**Entorno probado:**
- Frontend: https://logistique-les-lions.vercel.app
- API: https://logistique-les-lions-api.onrender.com
- HEAD probado: `e616b9e` (master)

---

## 1. Secciones de prueba

| Sección | Estado | Resumen |
|---|---|---|
| S4 — Landing y rutas públicas | ⚠️ | Rutas responden 200; 24 `.reveal` con IntersectionObserver y redirect post-registro a `/` son warnings. |
| S5 — Auth (registro, login, refresh, guards) | ✅ | Login, refresh 401→retry, guestGuard, adminGuard y authGuard funcionan tras fix `46c71c0`. |
| S6 — CRUD Vehículos | ✅ | POST 201, PUT 204, DELETE 204 con `?requesterId=`, filtros aplicados correctamente. |
| S7 — Dashboard y perfil | ✅ | PUT `/auth/me` 204, datos persistidos y recargados. |
| S8 — Favoritos | ⚠️ | Solo existe redirect `/favoritos → /perfil`; feature real no implementada. |
| S9 — Mensajería SignalR | ✅ | Handshake WS a `/hubs/chat?access_token=` 101, `ReceiveMessage` bidireccional verificado end-to-end. |
| S10 — Panel admin | ✅ | Layout y secciones cargan con `adminGuard`. |
| S11 — Smoke módulos adicionales | ✅ | 16 rutas públicas probadas, todas 200. |

---

## 2. Fixes aplicados durante el run

| SHA | Mensaje | Archivo(s) | Causa raíz |
|---|---|---|---|
| `81bd495` | fix(vercel): desactivar framework preset y excluir assets del rewrite SPA | `vercel.json` | El preset de Vercel reescribía assets estáticos a `index.html` rompiendo la SPA. |
| `71cccb5` | fix(frontend): añadir `<base href="/">` | `frontend/src/index.html` | Sin `<base>`, los chunks cargaban relativos a la URL actual (ej. `/vehiculos/main-xxx.js`) y 404. |
| `fc71af0` | fix(routes): redirects `/dashboard → /mis-vehiculos` y `/favoritos → /perfil` | `frontend/src/app/app.routes.ts` | Links pre-existentes a rutas obsoletas generaban 404. |
| `46c71c0` | fix(routes): `authGuard` estático en `/mis-vehiculos` | `frontend/src/app/app.routes.ts` | El `canActivate` usaba dynamic import → devolvía un `Promise` (siempre truthy) y la ruta quedaba sin protección real. |
| `e616b9e` | fix(signalr): leer JWT de `?access_token=` en handshake WebSocket a `/hubs/*` | `backend/src/LogistiqueLesLions.API/Extensions/IdentityExtensions.cs` (`JwtBearerEvents.OnMessageReceived`) | El navegador no puede añadir `Authorization` al upgrade WebSocket; SignalR envía el token por query string y ASP.NET Core Identity no lo leía. |

Ningún fix adicional requerido durante la re-verificación de S9.

---

## 3. Warnings no bloqueantes

1. **S4 — 24 elementos `.reveal` con `opacity:0`** en la home, revelados vía `IntersectionObserver`. Contra la regla del `CLAUDE.md` del proyecto (`animate-fadeInUp` con `animation-fill-mode: forwards` para contenido crítico).
2. **S5 — Redirección post-registro** va a `/` en vez de `/mis-vehiculos`. Esperado: aterrizar al área autenticada.
3. **S6 — `DELETE /api/v1/vehicles/{id}` requiere `?requesterId=` explícito.** Debería derivarse del `sub` del JWT en el backend — exponerlo en querystring rompe Clean Architecture y es un problema de seguridad (puede ser spoofeable si falta una comprobación adicional).
4. **S8 — Feature de favoritos no implementada.** Solo hay redirect a `/perfil`. Pendiente de desarrollo.
5. **S9 — Hub de chat sólo implementa `SendMessage` + `ReceiveMessage` + `MessageSent`.** Los eventos `MessageRead` y `UserTyping` mencionados en el `CLAUDE.md` **no existen en `ChatHub.cs`**. La reconexión automática sí está habilitada (`withAutomaticReconnect()` en `messaging.service.ts`). Pendiente de implementar read-receipts y typing indicators.
6. **S4 — `/vehiculos/publicar`** en footer apunta a ruta obsoleta (actual es `/vehiculos/nuevo`).

---

## 4. Errores bloqueantes sin resolver

Ninguno. Todos los flujos core de producción están operativos.

---

## 5. Verificación S9 (re-test post `e616b9e`)

### 5.1 Prueba de handshake (curl)

```
POST /hubs/chat/negotiate?negotiateVersion=1                         → 401
POST /hubs/chat/negotiate?negotiateVersion=1  (Authorization header) → 200
POST /hubs/chat/negotiate?negotiateVersion=1&access_token=<jwt>      → 200
```

La tercera es la que fallaba antes del fix: confirma que `OnMessageReceived` ahora copia `?access_token=` a `context.Token` para todas las rutas que empiezan por `/hubs/`.

### 5.2 End-to-end con `@microsoft/signalr` en browser

Desde un contexto Playwright autenticado, se cargó `@microsoft/signalr@8.0.0` y se establecieron dos conexiones WebSocket paralelas con los JWT de `test.user@lll.dev` (u1, dueño del vehículo `07ed25b3…`) y `test.user2@lll.dev` (u2).

| Paso | Resultado |
|---|---|
| `connU1.start()` | `Connected` |
| `connU2.start()` | `Connected` |
| `connU2.invoke('SendMessage', u1, vehicleId, body)` | Sin error. |
| Evento `ReceiveMessage` en u1 | ✅ 1 mensaje, payload correcto (`messageId`, `senderId=u2`, `vehicleId`, `body`, `createdAt`). |
| `connU1.invoke('SendMessage', u2, vehicleId, body)` | Sin error. |
| Evento `ReceiveMessage` en u2 | ✅ 1 mensaje, payload correcto (`senderId=u1`). |
| `connU1.stop()` / `connU2.stop()` | OK. |

Messages persistidos en BBDD (IDs `16429e97-…` y `cfad81c7-…`). Routing vía `Clients.User(recipientId)` + `OnConnectedAsync` añadiendo el connection al grupo con `userId` funciona correctamente.

---

## 6. Estado final del deploy

| Componente | Estado | Commit |
|---|---|---|
| `master` (git HEAD) | ✅ | `e616b9e` |
| Render (API) | ✅ Live | `e616b9e` — verificado por `/health/live` → `Healthy` y por el handshake SignalR con `?access_token=` devolviendo 200. |
| Vercel (frontend) | ✅ Live | `e616b9e` — páginas responden 200, assets resueltos, auth guards funcionando. |
| Neon (PostgreSQL) | ✅ | Mensajes persistidos durante el test S9. |

---

## 7. Conclusión

**Producción estable para los flujos core.** Auth completo (login/refresh/guards), CRUD de vehículos, perfil, panel admin y mensajería en tiempo real funcionan correctamente tras los 5 fixes aplicados durante el run. El fix crítico `e616b9e` desbloquea SignalR en producción al permitir la autenticación JWT durante el upgrade WebSocket.

**Áreas pendientes (no bloqueantes):**
- Implementar feature real de **favoritos**.
- Implementar eventos `MessageRead` y `UserTyping` en `ChatHub` (documentados en `CLAUDE.md` pero no implementados).
- Cambiar el patrón `.reveal` + `IntersectionObserver` en la home por `animate-fadeInUp` (conforme a la regla del `CLAUDE.md`).
- Derivar `requesterId` del JWT en `DELETE /api/v1/vehicles/{id}` (eliminar querystring).
- Redirigir post-registro a `/mis-vehiculos`.
- Corregir link `/vehiculos/publicar` → `/vehiculos/nuevo` en el footer.

**Recomendación:** apto para uso en producción para los casos de uso cubiertos. Priorizar la implementación de read-receipts/typing y favoritos antes del siguiente release mayor.

---

## 8. Verificación feature B (commits 2fb5598 + 0fb484c)

**Fecha:** 2026-04-14
**Alcance:** DashboardComponent y FavoritesComponent reales sustituyendo los redirects temporales de `fc71af0`.

### 8.1 Deploy
- Render API: operativo, sirviendo commit `0fb484c` (fix de EF Core). Endpoint `GET /api/v1/vehicles/favorites?userId=...` responde HTTP 200 con `[]` (verificado vía curl con JWT del usuario de prueba).
- Vercel frontend: operativo, las rutas `/dashboard` y `/favoritos` ya no redirigen y renderizan los componentes reales.

### 8.2 Bug encontrado y corregido
- `GetMyFavoritesQueryHandler`: los `.Include(Make/Model/Images)` aparecían encadenados tras `.Select(s => s.Vehicle)`. EF Core 9 no puede traducir un `Include` sobre el resultado de un `Select` y lanzaba `InvalidOperationException` al ejecutar la query → la API devolvía 500 al entrar en `/favoritos`. Fix en `0fb484c`: eliminados los 3 `Include` redundantes; el `Select` final proyectado a `VehicleListDto` ya genera los JOINs necesarios a `VehicleMakes`, `VehicleModels` e `VehicleImages` en el SQL traducido.

### 8.3 Dashboard `/dashboard`
- Saludo: **"Bienvenido, Test QA UserEdited"** (nombre real del usuario autenticado, no hardcodeado).
- Stats con contadores reales (no `—`):
  - **Mis vehículos publicados: 1** (coincide con el vehículo creado por el usuario en sesiones previas)
  - **Vehículos en favoritos: 0** (estado inicial tras el bug fix)
- 4 quick actions presentes y linkadas correctamente: Publicar vehículo (`/vehiculos/nuevo`), Mis vehículos (`/mis-vehiculos`), Favoritos (`/favoritos`), Mensajes (`/mensajes`).
- Requests de red verificadas:
  - `GET /api/v1/vehicles?sellerId=dc6d2292-...&pageSize=1` → **200**
  - `GET /api/v1/vehicles/favorites?userId=dc6d2292-...` → **200**
- Consola del navegador: **sin errores ni warnings**.

### 8.4 Favoritos — flujo completo
- **Marcar desde detalle** (`/vehiculos/tesla-model-y-2023-12`, vehicleId `172d881b-8b78-4061-9ab5-806bc98a4707`): click en botón "Añadir a favoritos" → `POST /api/v1/vehicles/{id}/favorite?userId=...` → **200**.
- **Listar en `/favoritos`**:
  - Título de la página: **"Mis favoritos"**.
  - `GET /api/v1/vehicles/favorites?userId=...` → **200** devolviendo el item recién guardado.
  - Card renderizada con título ("Tesla Model Y 2023"), subtítulo ("2023 · DE"), precio ("49,500 EUR") y thumbnail del vehículo. Botones "Ver anuncio" y "Quitar de favoritos" presentes.
- **Quitar in-place**: click en el botón rojo "Quitar de favoritos" → `POST /api/v1/vehicles/{id}/favorite?userId=...` (toggle) → **200**. El item desaparece del listado sin recargar la página y se muestra el empty state **"Aún no has guardado ningún vehículo — Cuando marques un vehículo como favorito, aparecerá aquí."** con CTA "Explorar vehículos".
- Consola: **sin errores**.

### 8.5 Veredicto

Feature B (Dashboard y Favoritos reales) verificada **apta para producción** tras el fix `0fb484c`. El endpoint de favoritos, roto por una traducción LINQ inválida en el handler, responde ahora correctamente. El dashboard muestra datos reales del usuario con contadores funcionales, y el flujo end-to-end de favoritos (marcar desde detalle → listar → quitar in-place) funciona sin errores de red ni consola. El punto "Implementar feature real de favoritos" de la sección 7 queda **resuelto**.
