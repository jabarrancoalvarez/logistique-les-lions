# Módulos legacy — pendientes de decisión

Inventario de todo lo que existe hoy en la aplicación y **no aparece** en la especificación
`Yoon u Auto DOC APP.md`. Son restos del producto anterior (*Logistique Les Lions*: import/export
multi-país con tramitación aduanera).

> **Decisión tomada:** no se toca nada de esta lista durante la adaptación al documento.
> Al terminar (parte P35) se decide módulo por módulo: eliminar, adaptar o conservar.

**La adaptación ya está cerrada: toca decidir.** Marca cada bloque con `ELIMINAR`,
`ADAPTAR` o `CONSERVAR` en la columna de la derecha. Las decisiones que no son de legacy
están en [`DECISIONES-PENDIENTES.md`](DECISIONES-PENDIENTES.md).

## Comprobado en producción el 16/08/2026

Todo esto **sigue vivo y accesible** en el despliegue actual:

| Qué | Estado real |
|---|---|
| `/precios` | Viva. Planes de **49 € y 199 €/mes**. Título «Precios — Yoon U Auto» |
| `/transporte` | Viva, en español, con importes en **€**. «Red de transporte… en toda Europa» |
| `/financiacion` | Viva, en español, con importes en **€** |
| `/tramitacion` | Viva, en español. «Importa y exporta vehículos sin complicaciones» |
| `/inspectores` | Viva, en español |
| `/guias/importacion` | Viva. «…importar un vehículo **a España** desde cualquier país» |
| `/tracking` | Viva, en español |
| `/concesionarios` | Viva, en español. «Si eres un **dealer**, regístrate…» |
| `/admin/procesos` | ⚠️ **En el menú lateral del backoffice**, en español |
| `/admin/incidencias` | ⚠️ **En el menú**, en español y con enums crudos: «Medium», «Open», «Resolved» |
| `/admin/partners` | ⚠️ **En el menú**. Datos europeos: «Gestoría Iberia», «Carfax Europe Inspectors». Título de pestaña «Marketplace — Admin» |
| `GET /vehicles/facets` | Devuelve **500** en producción. El frontend no lo llama |

Las públicas solo se alcanzan por URL directa (están fuera del menú desde P25), pero
responden 200 y las indexa cualquiera. **Las tres del backoffice sí están en el menú**: son
lo primero que ve un administrador al entrar.

---

## 1. Frontend — rutas y features

| Ruta | Feature | Qué hace | Observación |
|---|---|---|---|
| `/tramitacion` | `features/compliance` | Home, wizard, checklist documental, estimador de costes, guía de homologación, tracker de proceso | Muy extenso. El doc solo conserva el concepto de *estado aduanero* como campo del anuncio |
| `/transporte` | `features/transport` | Página de transporte internacional | No existe en el doc |
| `/financiacion` | `features/financing` | Página de financiación | No existe en el doc |
| `/inspectores` | `features/inspectors` | Inspectores certificados | El doc tiene checklist de inspección **privada del usuario**, no inspectores de la plataforma |
| `/guias/*` | `features/guides` | Guías de importación, exportación y homologación | No existe en el doc |
| `/precios` | `features/pricing` | Planes de precio | ⚠️ Contradice el doc: *todas las funcionalidades para usuarios son gratuitas* |
| `/concesionarios` | `features/dealers` | Listado de concesionarios | El doc reduce «Professionnel» a un campo del perfil sin interfaz propia |
| `/tracking` | `features/tracking` | Seguimiento público de trámite | No existe en el doc |
| `/pagos`, `/valoraciones` | `shared/coming-soon` | Placeholders | Sustituibles por el concepto *Prochainement* del doc |
| `/legal/*` | `features/legal` | Aviso legal, privacidad, cookies, términos, RGPD | ⚠️ **Conservar la ruta, tirar el contenido.** El doc las referencia, pero hoy están **en español y describen una sociedad española** («Yoon U Auto, S.L.», Madrid, Ley 34/2002). Ver [`DECISIONES-PENDIENTES.md`](DECISIONES-PENDIENTES.md) §1.1 |

### Componentes de la landing
| Componente | Observación |
|---|---|
| `landing/country-map` | Mapa multi-país. Sin sentido en una plataforma centrada en Senegal |
| `landing/newsletter` | No aparece en el doc |
| `landing/how-it-works` | Adaptable a las 3 etapas del doc |
| `landing/stats-counters` | Adaptable |
| `landing/hero-search` | ✅ Adaptable al buscador del Marketplace |
| `landing/featured-vehicles` | ✅ Adaptable |
| `landing/cookie-consent` | ✅ Conservar |

---

## 2. Backend — endpoints

| Endpoint | Observación |
|---|---|
| `ComplianceEndpoints` | Requisitos por país, estimación de costes, plantillas, procesos, documentos, incidencias |
| `CountryEndpoints` | Catálogo de países soportados |
| `PublicTrackingEndpoints` | Seguimiento público por referencia |
| `NewsletterEndpoints` | Suscripción a newsletter |
| `ExportEndpoints` | CSV de vehículos + PDF de albarán por proceso. El CSV puede reutilizarse en Statistiques |

---

## 3. Backend — features CQRS

- `Features/Compliance` (Commands + Queries)
- `Features/Countries` (Queries)
- `Features/PublicTracking` (Queries)
- `Features/Marketplace` (`CreatePartner`, `GetPartners`) — «Marketplace» aquí significa *partners de servicios*, no el marketplace de vehículos del doc. **Ojo con la colisión de nombres.**

---

## 4. Backend — entidades de dominio

| Entidad | Observación |
|---|---|
| `Country` | Multi-país. El doc es mono-país (Senegal) con regiones y ciudades |
| `CountryRequirement` | Requisitos documentales por país |
| `CustomsTariff` | Aranceles aduaneros |
| `DocumentTemplate` | Plantillas de documentos |
| `HomologationRequirement` | Requisitos de homologación |
| `ImportExportProcess` | Proceso de importación/exportación |
| `ProcessDocument` | Documentos del proceso |
| `ProcessIncident` | Incidencias del proceso |
| `ServicePartner` | Partners (transportistas, aduaneros, inspectores) |
| `NewsletterSubscriber` | Suscriptores |
| `VehicleDocument` | ⚠️ **Posible reutilización** en `Mon Garage → Documents` |
| `VehicleHistory` | ⚠️ **Posible reutilización** en el historial del vehículo |

---

## 5. Enums a revisar

| Enum | Observación |
|---|---|
| `UserRole` (Buyer/Seller/Dealer/Admin/Moderator) | El doc solo define Usuario y Administrador |
| `ProcessStatus`, `ProcessType`, `IncidentSeverity`, `PartnerType`, `DocumentStatus` | Ligados a los módulos de tramitación |
| `VehicleDocumentType` | ⚠️ Reutilizable en Mon Garage |

---

## 6. Otros elementos del producto anterior

- **Generación de descripciones con IA** (`GenerateVehicleDescriptionCommand`, Anthropic API).
  ⚠️ Contradice el doc: *«Yoon u Auto no modificará ni generará mediante IA esta descripción»*.
  🔴 **Ya desactivado en P3**: el handler devuelve `Vehicle.AiDescriptionDisabled` y el botón
  «Generar con IA» se ha retirado del formulario de alta. El código sigue en su sitio a la
  espera de la decisión de P35.
- **Extracción IA de documentos** (`ExtractVehicleDocument`, botón «Subir documento» del paso 1
  del formulario). Sigue activo: no lo prohíbe el documento, pero tampoco lo contempla.
- **Panel admin actual**: secciones `procesos`, `incidencias`, `partners` — ligadas a la tramitación.
- **Planes / límites por rol** (`User` hasta 3 anuncios, `Dealer` ilimitado).
  ⚠️ Contradice el doc: todo gratuito e ilimitado.
- **Multi-divisa** (`Currency` en `Vehicle` y `Country`). El doc trabaja únicamente en FCFA.

---

## 7. Hoja de decisión

Un bloque por línea. Marca `ELIMINAR`, `ADAPTAR` o `CONSERVAR`.

| # | Bloque | Alcance | Decisión |
|---|---|---|---|
| 1 | **Planes de precio** (`/precios`) | Frontend | |
| 2 | **Concesionarios** (`/concesionarios`) | Frontend | |
| 3 | **Tramitación aduanera** (`/tramitacion` + wizard, checklist, estimador, guía, tracker) | Frontend + `ComplianceEndpoints` + `Features/Compliance` + 8 entidades | |
| 4 | **Transporte** (`/transporte`) | Frontend | |
| 5 | **Financiación** (`/financiacion`) | Frontend | |
| 6 | **Inspectores** (`/inspectores`) | Frontend | |
| 7 | **Guías** (`/guias/*`) | Frontend | |
| 8 | **Tracking público** (`/tracking`) | Frontend + `PublicTrackingEndpoints` | |
| 9 | **Placeholders** (`/pagos`, `/valoraciones`) | Frontend. Sustituibles por *Prochainement* | |
| 10 | **Componentes huérfanos de la portada** (`country-map`, `newsletter`, `stats-counters`) | Frontend. Ya no se usan | |
| 11 | **Backoffice heredado** (`/admin/procesos`, `/admin/incidencias`, `/admin/partners`) | ⚠️ **Sigue en el menú** | |
| 12 | **`dashboard-kpis.component.ts`** | Huérfano, no lo usa nadie | |
| 13 | **`CountryEndpoints` + `Features/Countries`** | Backend. El doc es mono-país | |
| 14 | **`NewsletterEndpoints` + `NewsletterSubscriber`** | Backend | |
| 15 | **`ExportEndpoints`** | Backend. ⚠️ El CSV **puede reutilizarse** en Statistiques | |
| 16 | **`GET /vehicles/facets`** | Backend. **Devuelve 500 en producción** | |
| 17 | **`Features/Marketplace` (partners)** | Backend. ⚠️ Colisión de nombres con el marketplace de vehículos | |
| 18 | **Generación de descripciones con IA** | Backend. Ya desactivada, el código sigue. El doc **la prohíbe** | |
| 19 | **Extracción IA de documentos** | Backend + paso 1 del formulario. **Sigue activa** | |
| 20 | **Entidades de tramitación** (`Country`, `CountryRequirement`, `CustomsTariff`, `DocumentTemplate`, `HomologationRequirement`, `ImportExportProcess`, `ProcessDocument`, `ProcessIncident`, `ServicePartner`) | Dominio + migraciones | |
| 21 | **`VehicleDocument` y `VehicleHistory`** | Dominio. ⚠️ **Revisar solape con Mon Garage antes de tocar** | |
| 22 | **Multi-divisa** (`Currency` en `Vehicle` y `Country`) | Dominio. El doc es solo FCFA | |
| 23 | **Contenido de las páginas legales** | Frontend. Hay que **reescribirlo**, no traducirlo | |
| 24 | **Namespaces `LogistiqueLesLions.*` → `YoonUAuto.*`** | Todo el backend. Aplazado a propósito | |
| 25 | **Rutas en español** (`/mis-vehiculos`, `/mi-garaje`, `/mis-busquedas`, `/ajustes`, `/mis-pedidos`…) | Frontend. Traducirlas rompe los enlaces ya compartidos | |

> Al eliminar entidades hay que **generar la migración correspondiente**, y las tablas
> tienen datos sembrados. Nada de esto es borrar archivos y ya.
