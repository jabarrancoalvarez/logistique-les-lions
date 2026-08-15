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
| 8 | **Puntos de fidelización** | El documento los menciona sin concretarlos. Pendiente de definir | P16a, se retoma en P34 |

## 🟡 Calidad y mantenimiento

| # | Asunto | Detalle | Origen |
|---|---|---|---|
| 9 | **El frontend no tiene ni un test** | Carencia anterior a la migración. `ng test` además necesita un navegador instalado | Anterior |
| 10 | **Namespaces `LogistiqueLesLions.*`** | Renombrado a `YoonUAuto.*` aplazado a propósito hasta el final | Decisión del usuario, P1 |
| ~~11~~ | ~~**Menú de usuario en español**~~ | ✅ **Resuelto en P25**: navegación, menú del avatar y panel personal en francés | P25 |
| 12 | **Rutas en español** | `/mis-negociaciones`, `/mi-garaje`, `/mis-busquedas`, `/ajustes`… conviven con una interfaz en francés. Decidir si se traducen (rompe enlaces existentes) o se dejan | Transversal |
| 17 | **`dashboard-kpis.component.ts` huérfano** | Renderiza KPIs del producto anterior (procesos, lead time, incidencias). Al reescribir el Tableau de bord dejó de usarse. No se borra porque su módulo sigue pendiente de decisión | P26, ligado a **P35** |
| 19 | **Estadísticas agregadas en memoria** | `GetStatisticsQuery` trae los anuncios activos, las regiones de todos los usuarios y todas las búsquedas guardadas al proceso para agruparlos con LINQ. Es correcto y agnóstico del proveedor, pero con decenas de miles de filas hay que llevar las agregaciones a SQL (`GROUP BY`, y funciones JSONB para `filters_json`) o cachear el resultado | P33 |
| 13 | **Avisos del compilador** | `CS9107` en los handlers con clase base y parámetros primarios; `RouterLink` sin usar en `AdminDashboardComponent` | Varias |

## 🔵 Decisiones pendientes

| # | Asunto | Detalle | Origen |
|---|---|---|---|
| 16 | **Módulos legacy fuera del menú** | Tramitación, concesionarios, etc. siguen accesibles por URL y desde el pie, pero ya no están en la navegación principal. Va con la decisión de abajo | P25 |
| 14 | **Módulos legacy** | Tramitación, transporte, financiación, inspectores, guías, precios, concesionarios y tracking siguen intactos. Inventario en [`MODULOS-LEGACY.md`](MODULOS-LEGACY.md) | Decisión del usuario, se resuelve en **P35** |
