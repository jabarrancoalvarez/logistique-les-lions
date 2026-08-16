# Pendientes técnicos — resolver al final de la migración

> Cuestiones detectadas durante la migración a Yoon u Auto que **no pertenecen a ninguna
> parte concreta** del plan, o que se decidió aplazar conscientemente. Se abordan cuando
> las 35 partes estén cerradas.
>
> Las desviaciones respecto al documento funcional van en su propia tabla dentro de
> [`MIGRACION-YOON-U-AUTO.md`](MIGRACION-YOON-U-AUTO.md); aquí solo hay deuda técnica.

Última actualización: **2026-08-15** (tras la Parte 32).

---

## 🔴 Bloqueantes antes de producción

| # | Asunto | Detalle | Origen |
|---|---|---|---|
| 1 | **Migraciones sin aplicar** | Ninguna migración de la migración Yoon u Auto se ha ejecutado contra PostgreSQL. La app no arranca en local y **ninguna parte se ha podido verificar a mano**. Al aplicarlas hay que revisar sobre todo los índices únicos parciales (`contracts`, `garage_vehicles`), que el proveedor en memoria de los tests no valida | Decisión del usuario, P1 |
| 2 | **Almacenamiento efímero en Render** | El disco local se pierde en cada reinicio: se van las fotos de los anuncios y **la documentación de Mon Garage**. Hay que implementar `IStorageService` sobre S3 / Cloudflare R2, incluidas las dos vías (pública y privada) | P18 (el problema es anterior) |
| 3 | **Correo sin configurar** | Existe `ResendEmailSender`, pero sin `Email:ApiKey` se usa `ConsoleEmailSender` y nada sale del servidor. Hay que configurarlo en Render | P2, usado en P32 |

## 🟠 Funcionalidad incompleta

| # | Asunto | Detalle | Origen |
|---|---|---|---|
| 4 | **Fotografías en el chat** | El documento las contempla dentro de la negociación. El chat solo admite texto | P13 / P14 |
| 5 | **«Modifier» en una búsqueda guardada** | Solo permite cambiar el nombre, no los criterios | P9 |
| ~~6~~ | ~~**Alerta de anuncio nuevo**~~ | ✅ **Resuelto en P24**: la alerta salta al publicar (`Brouillon → Actif`), una sola vez, y no al volver de una pausa | P9 → P24 |
| 7 | **Selección del comparador en `localStorage`** | No viaja entre dispositivos | P10 |
| 15 | **«Supprimer mon compte»** | Paramètres no lo ofrece: no existe endpoint de baja de cuenta. Tampoco hay pantallas de privacidad ni de seguridad | P25 |
| 18 | **Envío masivo de comunicaciones** | Una comunicación a «todos» crea una notificación por usuario en una sola operación. Con miles de cuentas debe pasar a un proceso en segundo plano | P32 |
| ~~8~~ | ~~**Puntos de fidelización**~~ | ✅ **Resuelto en P34**: libro de movimientos append-only, +100 por venta verificada (configurable), ajuste manual con motivo y compensación al invalidar | P16a → P34 |

## 🟡 Calidad y mantenimiento

| # | Asunto | Detalle | Origen |
|---|---|---|---|
| 22 | **SignalR no se rehace al refrescar el token** | El interceptor renueva el token de acceso y las llamadas HTTP se reintentan, pero la conexión del hub se queda caída hasta recargar la página: las notificaciones en vivo dejan de llegar a los 15 minutos | Detectado probando en producción |
| 23 | **Negociaciones huérfanas del seed anterior** | El tableau de bord cuenta 11 negociaciones abiertas que apuntan a los anuncios europeos ya retirados. No estorban, pero falsean la actividad | Detectado probando en producción |
| 24 | **Contraseña de demostración en el historial de git** | El primer commit del reseed sembró las cuentas de vendedor con una contraseña fija en un repositorio público. Ya no abre ninguna cuenta —ahora son aleatorias— pero el commit sigue ahí | Introducido y corregido en las pruebas |
| 25 | **Una sola sesión por cuenta** | `UserProfile.RefreshToken` es una única columna: iniciar sesión en un segundo dispositivo invalida en silencio la sesión del primero, que acaba expulsado al caducar su token de acceso. Para una plataforma que se usa desde el móvil y desde el ordenador conviene decidir si se admiten sesiones simultáneas —lo que exige una tabla de refresh tokens— o se documenta como comportamiento buscado | Detectado probando en producción |
| 21 | **No hay tests de la capa API** | Los 476 tests son de Application: nadie prueba el binding de la query string ni las rutas. El fallo de `[AsParameters]` con `int` no anulable (400 con cuerpo vacío en `/vehicles/count`) llegó a producción por esto. Haría falta `WebApplicationFactory` | Detectado probando en producción |
| 9 | **El frontend no tiene ni un test** | Carencia anterior a la migración. `ng test` además necesita un navegador instalado | Anterior |
| 10 | **Namespaces `LogistiqueLesLions.*`** | Renombrado a `YoonUAuto.*` aplazado a propósito hasta el final | Decisión del usuario, P1 |
| ~~11~~ | ~~**Menú de usuario en español**~~ | ✅ **Resuelto en P25**: navegación, menú del avatar y panel personal en francés | P25 |
| 12 | **Rutas en español** | `/mis-negociaciones`, `/mi-garaje`, `/mis-busquedas`, `/ajustes`… conviven con una interfaz en francés. Decidir si se traducen (rompe enlaces existentes) o se dejan | Transversal |
| 20 | **Componentes de portada huérfanos** | `country-map`, `newsletter` y `stats-counters` ya no se usan tras reescribir la portada en P35. Se decide con el resto del legacy | P35 |
| 17 | **`dashboard-kpis.component.ts` huérfano** | Renderiza KPIs del producto anterior (procesos, lead time, incidencias). Al reescribir el Tableau de bord dejó de usarse. No se borra porque su módulo sigue pendiente de decisión | P26, ligado a **P35** |
| 19 | **Estadísticas agregadas en memoria** | `GetStatisticsQuery` trae los anuncios activos, las regiones de todos los usuarios y todas las búsquedas guardadas al proceso para agruparlos con LINQ. Es correcto y agnóstico del proveedor, pero con decenas de miles de filas hay que llevar las agregaciones a SQL (`GROUP BY`, y funciones JSONB para `filters_json`) o cachear el resultado | P33 |
| 13 | **Avisos del compilador** | `CS9107` en los handlers con clase base y parámetros primarios; `RouterLink` sin usar en `AdminDashboardComponent` | Varias |

## 🔵 Decisiones pendientes

| # | Asunto | Detalle | Origen |
|---|---|---|---|
| 16 | **Módulos legacy fuera del menú** | Tramitación, concesionarios, etc. siguen accesibles por URL y desde el pie, pero ya no están en la navegación principal. Va con la decisión de abajo | P25 |
| 14 | **Módulos legacy** | Tramitación, transporte, financiación, inspectores, guías, precios, concesionarios y tracking siguen intactos. Inventario en [`MODULOS-LEGACY.md`](MODULOS-LEGACY.md) | Decisión del usuario, se resuelve en **P35** |
