# Verificación al aplicar las migraciones y subir a producción

> Todo lo que **no se ha podido comprobar** durante la migración a Yoon u Auto porque las
> migraciones se aplican todas juntas al final (decisión del usuario) y porque el proveedor
> en memoria de los tests no reproduce el comportamiento real de PostgreSQL.
>
> Se va rellenando al cerrar cada parte. Deuda técnica general en
> [`PENDIENTES-TECNICOS.md`](PENDIENTES-TECNICOS.md).

Última actualización: **2026-08-15** (tras la Parte 32).

---

## 1. Antes de aplicar nada

- [ ] **Copia de seguridad de la base de datos de producción (Neon).** Varias migraciones
      transforman datos existentes; sin copia no hay vuelta atrás.
- [ ] Aplicar primero **en local** (`docker-compose up -d` + `dotnet ef database update`)
      sobre una copia de los datos de producción, no sobre una base vacía: los problemas
      de estas migraciones aparecen con datos, no sin ellos.
- [ ] Revisar el SQL generado antes de ejecutarlo:
      `dotnet ef migrations script --idempotent --output migracion.sql`

## 2. Migraciones reescritas a mano — revisar con lupa

Estas cuatro no salieron bien de EF y se corrigieron manualmente. **Son las que más
riesgo tienen.**

| Migración | Qué se corrigió | Qué comprobar después |
|---|---|---|
| `ModeleUtilisateurYoonUAuto` (P2) | EF ponía `DropColumn` antes de `AddColumn` y dedujo un renombrado falso `is_active` → `allow_whats_app_contact` | Ningún usuario ha perdido teléfono, correo ni tipo de cuenta. Los teléfonos quedan normalizados a `+221XXXXXXXXX` |
| `ModeleVehiculeYoonUAuto` (P3) | Migración de datos de enums y `public_reference` por secuencia | Todos los anuncios tienen `public_reference` con formato `YU#####` y **sin duplicados**. Los enums traducidos (`FuelType`, `BodyType`…) no han dejado filas con el valor antiguo |
| `Negociations` (P13) | EF quería **borrar** la tabla `conversations`; se cambió por `RenameTable` + SQL para PK, 3 FK y 3 índices | Las conversaciones anteriores siguen ahí, con sus mensajes, y las FK apuntan bien. `SELECT count(*) FROM messaging.negotiations` ≈ conversaciones que había |
| `Offres` (P14) | El `CreateTable` salió vacío tras regenerar y se escribió a mano | La tabla `offers` existe con todas sus columnas e índices |

## 3. Índices únicos parciales

El proveedor en memoria **no los valida**: solo se comprueban contra PostgreSQL de verdad.

- [ ] **`contracts.negotiation_id`** — `UNIQUE ... WHERE status <> 'Annule'`
      → Anular un contrato y crear otro para la misma negociación debe funcionar.
      → Crear dos contratos vivos en la misma negociación debe fallar.
- [ ] **`garage_vehicles.source_contract_id`** — `UNIQUE ... WHERE deleted_at IS NULL`
      → Retirar un vehículo del garaje y volver a añadirlo desde la misma compra debe funcionar.
      → Añadir dos veces el mismo contrato debe fallar.
- [ ] **`contracts.verification_code`** — `UNIQUE`, admite muchos `NULL`
      → Varios contratos sin validar (código `NULL`) deben poder convivir. Es el
        comportamiento estándar de PostgreSQL, pero conviene verlo.

## 4. Secuencias de referencias públicas

- [ ] `vehicles.vehicle_reference_seq` existe y arranca donde toca (`YU#####`)
- [ ] `vehicles.vehicle_request_reference_seq` (`YD#####`)
- [ ] `messaging.contract_reference_seq` (`YC#####`)
- [ ] `messaging.report_reference_seq` (`SG#####`)
- [ ] Crear dos anuncios **a la vez** no genera la misma referencia (es la razón de usar
      secuencias y no `count(*)`)

## 5. Comportamientos de borrado que solo existen en la base de datos

- [ ] **`maintenance_records.document_id` → `ON DELETE SET NULL`**
      Borrar un documento de Documents debe dejar la intervención viva **sin factura**,
      nunca arrastrarla. Es lo único de la Parte 19 que no puede probarse en los tests.
- [ ] Las cascadas (`negotiations` → mensajes/eventos/ofertas, `garage_vehicles` →
      imágenes/documentos/mantenimientos) no borran de más.
- [ ] Los filtros globales de soft delete siguen activos: nada eliminado aparece en las
      consultas normales.

## 6. Seeds y catálogos

- [ ] `price_indicator_settings` tiene su fila de parámetros. **Sin ella el indicador de
      precio no se muestra** (por diseño), lo que puede parecer un fallo.
- [ ] `valuation_settings` tiene su fila. Igual que arriba: sin datos suficientes Mon
      Garage dirá *«Pas assez de données»*, y en una base recién migrada con pocos
      anuncios **eso será lo normal**, no un error
- [ ] La estimación de valor se calcula por vehículo al abrir Mon Garage. Con muchos
      vehículos por usuario conviene mirar el tiempo de respuesta de `GET /api/v1/garage`
- [ ] Catálogo `vehicle_equipments` poblado y los enlaces de los anuncios existentes migrados
- [ ] Marcas y modelos (`vehicle_makes`, `vehicle_models`) intactos: Mon Garage y los
      anuncios dependen de ellos
- [ ] Los `HasData` con timestamps fijos no generan `UpdateData` espurios en la siguiente
      migración

## 7. Almacenamiento de archivos

- [ ] Crear el directorio **`private-uploads/`** o definir `Storage__PrivatePath` en Render
- [ ] Comprobar que un documento de Mon Garage **no** es accesible por URL directa
      (`/uploads/...`) y sí por `GET /api/v1/garage/documents/{id}/file` con token
- [ ] Comprobar que el token de **otro usuario** recibe 403 en ese mismo endpoint
- [ ] ⚠️ **El disco de Render es efímero**: al reiniciar se pierden fotos y documentos.
      Ver punto 2 de `PENDIENTES-TECNICOS.md`

## 8. PDF y QR del contrato

- [ ] ⚠️ **QuestPDF en Linux necesita fuentes del sistema.** El contenedor de Render debe
      tener `libfontconfig1` (y `libfreetype6`); si no, la generación del PDF revienta en
      producción aunque funcione en Windows. Es el riesgo más probable de la Parte 16b
- [ ] Descargar el PDF de un contrato validado y abrirlo
- [ ] Escanear el QR con un móvil: debe llevar a `{Frontend__Url}/verification/{código}`
      → verificar que `Frontend__Url` está bien puesta en Render
- [ ] La página de verificación funciona **sin sesión iniciada**

## 9. Rappels de Mon Garage (P20)

- [ ] El trabajo en segundo plano **`ReminderNotifierService`** arranca (log a los 45 s) y
      no revienta contra la base de datos real. Se ejecuta cada 6 h
- [ ] Un rappel con fecha vencida pasa a «À faire» y genera notificación en la campana
- [ ] Ese mismo rappel **no vuelve a avisar** en la siguiente vuelta (`notified_at`)
- [ ] Un rappel por kilometraje **no se mueve** por mucho tiempo que pase si el usuario no
      declara kilómetros nuevos — es un requisito explícito del documento
- [ ] Con **varias instancias** en Render el aviso sigue llegando una sola vez

## 10. Transparencia del historial (P23)

- [ ] Un anuncio **sin** transparencia configurada no expone nada:
      `GET /api/v1/vehicles/{id}/transparency` devuelve `null`
- [ ] Una factura compartida se descarga **sin sesión**; la misma factura deja de servirse
      en cuanto se desmarca la casilla
- [ ] El borrador creado desde Mon Garage reutiliza las URL de las fotos del garaje: al
      migrar a S3/R2 hay que comprobar que siguen resolviendo
- [ ] El índice único `vehicle_transparency.vehicle_id` no impide poner el mismo coche a
      la venta otra vez tras venderlo (se crea un anuncio nuevo, con su propia fila)

## 11. Mes annonces (P24)

- [ ] Publicar un borrador dispara la «Alerte nouveaux véhicules» **una sola vez**; pausar
      y reactivar no vuelve a dispararla
- [ ] Bajar el precio desde la acción rápida añade fila a `vehicle_price_history` y avisa
      a quienes lo tienen en Favoris
- [ ] «Dupliquer» genera referencia y slug nuevos sin chocar con el índice único de slug
- [ ] El listado incluye borradores y archivados **solo del propio usuario**

## 12. Backoffice (P26–P32)

- [ ] El «Tableau de bord» responde en un tiempo razonable: son ~25 conteos, y con la
      base llena conviene mirar si alguno necesita índice
- [ ] Suspender una cuenta impide iniciar sesión, y **al pasar la fecha vuelve sola** sin
      que nadie la reactive
- [ ] `admin_actions` conserva las filas: no hay borrado ni soft delete sobre esa tabla
- [ ] Un administrador no puede suspenderse a sí mismo ni a otro administrador
- [ ] Un anuncio ocultado por moderación **desaparece del buscador público** aunque su
      estado siga siendo «Actif», y el vendedor no puede reponerlo desde Mes annonces
- [ ] «Demander une correction» genera notificación al vendedor y no toca el anuncio
- [ ] Proponer un vehículo en una demande avisa al usuario («Nous avons trouvé un
      véhicule pour vous») y la solicitud pasa a «Véhicule proposé»
- [ ] La `FK` de `vehicle_requests.assigned_admin_id` es `ON DELETE SET NULL`: borrar un
      administrador no debe arrastrar solicitudes
- [ ] Leer una conversación desde el backoffice deja fila en `admin_actions` con el
      nombre del administrador y el motivo escrito
- [ ] Invalidar una venta verificada baja el contador `verified_sales_count` del vendedor
      (y no lo deja en negativo si se repite)
- [ ] Reportar un anuncio genera referencia `SG#####` y lo hace aparecer en el filtro
      «reportadas»; al resolver el signalement deja de aparecer
- [ ] Una comunicación «Tous» con muchos usuarios no tumba la petición: genera una
      notificación por destinatario en un solo `SaveChanges`. Con miles de cuentas habrá
      que pasarlo a un proceso en segundo plano

### Statistiques (P33)

- [ ] `/admin/statistiques` carga con datos reales y con las cuatro ventanas (7 j, 30 j,
      90 j, 12 mois)
- [ ] Con la base ya poblada, medir cuánto tarda: las agregaciones se hacen **en memoria**
      (pendiente técnico 19). Si tarda, llevarlas a SQL antes de que crezca más
- [ ] El bloque «Ce qu'on cherche et qu'on ne trouve pas» cruza búsquedas guardadas,
      demandes y anuncios activos. Comprobar con un modelo real que las tres cifras
      cuadran con lo que hay en base de datos
- [ ] Una búsqueda guardada con `filters_json` corrupto no debe dejar el panel en blanco
- [ ] Las medianas de precio, kilometraje y año se calculan sobre anuncios `Actif` y
      `Reserve`: verificar que no cuentan borradores ni archivados

## 13. Tiempo real y correo

- [ ] SignalR: las notificaciones de oferta y de contrato llegan en vivo a la campana
      (esto se añadió en P16a y nunca se ha probado contra un servidor real)
- [ ] Chat: `ReceiveMessage`, `MessageRead` y `UserTyping` siguen funcionando tras el
      renombrado de `conversations` a `negotiations`
- [ ] Correo: hoy solo existe `ConsoleEmailSender`, así que **no sale ningún correo**

## 14. Recorrido funcional completo (humo)

Un solo recorrido cubre casi toda la migración:

1. [ ] Registrarse con teléfono `+221...` e iniciar sesión
2. [ ] Publicar un anuncio → tiene referencia `YU#####`
3. [ ] Buscar, filtrar y guardar la búsqueda; marcar favorito
4. [ ] Desde otra cuenta: hacer una oferta → contraoferta → aceptar
5. [ ] Rellenar la checklist privada de inspección (la otra parte **no** debe verla)
6. [ ] Crear el contrato, enviarlo, pedir una modificación, corregir y validar
7. [ ] Comprobar: anuncio `Vendu`, negociación `Terminée`, +1 vente vérifiée
8. [ ] Descargar el PDF y verificar el QR
9. [ ] «Ajouter ce véhicule à Mon Garage» (solo debe verlo quien compra)
10. [ ] Subir un documento, registrar una intervención y enlazarle la factura
11. [ ] «Vendre ce véhicule» → sale un borrador con las fotos del garaje y **sin precio**
12. [ ] Compartir un entretien y su factura → verlos en el anuncio desde una sesión cerrada
