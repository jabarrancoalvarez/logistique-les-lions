# Módulos legacy — pendientes de decisión

Inventario de todo lo que existe hoy en la aplicación y **no aparece** en la especificación
`Yoon u Auto DOC APP.md`. Son restos del producto anterior (*Logistique Les Lions*: import/export
multi-país con tramitación aduanera).

> **Decisión tomada:** no se toca nada de esta lista durante la adaptación al documento.
> Al terminar (parte P35) se decide módulo por módulo: eliminar, adaptar o conservar.

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
| `/legal/*` | `features/legal` | Aviso legal, privacidad, cookies, términos, RGPD | ✅ **Conservar**: el doc los referencia en `Paramètres → Textos legales` |

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
