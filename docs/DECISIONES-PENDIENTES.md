# Decisiones pendientes

Todo lo que ha salido probando contra producción y **necesita que decidas tú**, no que se
programe sin más. Ordenado por lo que bloquea abrir al público.

- Lo encontrado y ya corregido está en [`../flujopruebas.md`](../flujopruebas.md) §11 y §12.
- Los módulos del producto anterior, uno a uno, en [`MODULOS-LEGACY.md`](MODULOS-LEGACY.md).
- La deuda técnica de fondo, en [`PENDIENTES-TECNICOS.md`](PENDIENTES-TECNICOS.md).

> Marca la casilla cuando la decisión esté tomada y anota al lado lo que se hace.

---

## 0. Correcciones ya desplegadas

- [x] **Visibilidad de anuncios**: un anuncio en `Brouillon`, `EnPause`, `Archive` o
      ocultado por moderación se servía entero a cualquiera con el enlace. Corregido y
      verificado en producción: ahora responde 404.
- [x] **Listado de negociaciones**: decía «12» y enseñaba 1. Corregido.

---

## 1. Bloqueantes para abrir al público

### 1.1 🔴 Las páginas legales describen una empresa española

Las cinco (`/legal/aviso-legal`, `privacidad`, `cookies`, `terminos`, `rgpd`) tienen el
título en francés y el cuerpo en español, y declaran:

> «Yoon U Auto, **S.L.** … Domicilio social: Calle de ejemplo, 123, 28001 **Madrid,
> España**. NIF: B-12345678. **Registro Mercantil de Madrid**», en cumplimiento de la
> **Ley 34/2002 (LSSI-CE)**.

La de RGPD invoca el Reglamento (UE) 2016/679 y la Ley Orgánica 3/2018. **Están enlazadas
desde el pie de todas las páginas.**

No se arregla traduciendo: el aviso legal de un marketplace senegalés declara una empresa
que no es la suya, en un país que no es el suyo, bajo una ley que no le aplica.

- [x] ✅ **RESUELTO el 16/08/2026: textos provisionales en francés.** Reescritas las cinco
      apuntando a la normativa senegalesa (loi n° 2008-12 y la CDP), con los datos de la
      sociedad como «[à compléter]» y un aviso destacado de que son provisionales.
- [ ] ⏳ **Queda**: rellenar razón social, domicilio, RCCM y NINEA reales, y que lo revise
      un abogado en Senegal antes de abrir al público.

### 1.2 🔴 Las fotos de Mon Garage se sirven sin autenticación

Van a `/uploads/`, la carpeta estática: `GET .../uploads/garage/<id>/<guid>.png` devuelve
**200 sin token**. Los documentos sí están bien (401, y la `StorageKey` no sale en el DTO).
En el código: `GarageEndpoints.cs:134` usa `UploadAsync`; las líneas 205 y 380, la versión
privada.

No es un descuido evidente: están en la carpeta pública **porque «Vendre ce véhicule» las
hereda en el anuncio**, y las fotos de un anuncio tienen que ser públicas.

- [x] ✅ **RESUELTO el 16/08/2026: opción 1.** Las fotos van al almacenamiento privado y
      se sirven por `GET /garage/images/{id}`, que comprueba el dueño antes de entregarlas.
      Al pulsar «Vendre ce véhicule» se **copian** —no se mueven— a la carpeta pública
      para el anuncio, así que retirar el anuncio no vacía el garaje.

      El frontend las pide como blob con el token, igual que ya hacía con los documentos
      y las fotos de intervención: una etiqueta `<img>` no envía cabeceras.

      La migración `PhotosPriveesDuGarage` retira las filas antiguas, que guardaban una
      ruta pública imposible de servir en privado. ⚠️ Los archivos sueltos siguen en el
      disco de Render hasta el próximo reinicio, que lo borra todo (pendiente nº 2).

### 1.3 🔴 Las notificaciones en vivo se mueren a los 15 minutos

Al caducar el token, el `negotiate` de SignalR recibe 401 y **el cliente no lo reintenta
nunca**. El token nuevo sí vale (repetido a mano: 200). Efecto: pasados 15 minutos las
notificaciones en tiempo real dejan de llegar **en silencio**, y solo vuelven al recargar.
Es el pendiente nº 22, ahora medido.

- [x] ✅ **RESUELTO el 16/08/2026: reconectar al renovar el token.** Eran dos fallos en
      los dos hubs (`/hubs/notifications` y `/hubs/chat`):
  1. `accessTokenFactory: () => token` **capturaba** el token al conectar, así que la
     reconexión automática de SignalR seguía presentando el caducado. Ahora se lee en
     cada negociación: `() => this.auth.accessToken()`.
  2. Si el arranque fallaba, se descartaba la conexión y **nadie volvía a intentarlo**.
     Ahora un `effect` sobre la señal del token rehace la conexión cuando el interceptor
     lo renueva, y la cierra al cerrar sesión.

### 1.4 Almacenamiento efímero y correo sin configurar

Ya estaban documentados, pero ahora hay datos reales encima.

- [x] ✅ **Disco de Render efímero** (pendiente nº 2) — **código listo el 16/08/2026,
      esperando el bucket.** Los archivos nunca estuvieron en Neon: la base guarda las
      filas y el contenedor de Render los ficheros, y ese contenedor se recrea en cada
      despliegue. Se ha implementado `ObjectStorageService`, compatible con S3, contra
      **Cloudflare R2** (10 GB gratis y sin coste de descarga).

      **Sigue desactivado**: mientras falte cualquiera de las claves, la aplicación vuelve
      sola al disco en lugar de no arrancar. Para encenderlo hacen falta cuatro pasos
      tuyos, en Cloudflare y en Render:

      1. Crear un bucket en Cloudflare R2.
      2. Exponer **solo el prefijo `public/`** (dominio público o dominio propio).
         ⚠️ Si se abre el bucket entero, la documentación privada de Mon Garage queda al
         alcance de cualquiera: el servicio separa `public/` y `private/` por prefijo.
      3. Crear un token de API con permiso de lectura y escritura sobre ese bucket.
      4. En Render → Environment, rellenar y cambiar el proveedor:
         - `Storage__Provider` → `r2`
         - `Storage__Bucket` → nombre del bucket
         - `Storage__ServiceUrl` → `https://<account-id>.r2.cloudflarestorage.com`
         - `Storage__AccessKey` y `Storage__SecretKey` → las del token
         - `Storage__PublicBaseUrl` → la URL pública del bucket

      ⚠️ Lo ya subido al disco **no se migra solo**: son datos de prueba y se vuelven a
      subir.
- [ ] ⏸️ **Correo — APLAZADO al final** (pendiente nº 3). Sin `Email__ApiKey` no sale
      ningún correo; las comunicaciones del backoffice solo llegan como notificación
      interna. Se retoma cuando exista la cuenta de Yoon u Auto.

      Ya corregido lo que sí bloqueaba: el remitente por defecto era
      `Logistique Les Lions <no-reply@logistiqueleslions.com>`, la marca anterior y un
      dominio ajeno.

      Al retomarlo, dos cosas que decidirán la solución:
      - ⚠️ **Desde un Gmail no se puede enviar.** El proveedor exige verificar el dominio
        y `gmail.com` no es nuestro. Hace falta un dominio propio para el `FromAddress`;
        el Gmail vale como dirección de contacto y recepción.
      - ❓ **¿Lee el correo el público senegalés?** La cuenta va por teléfono y el correo
        es opcional. Si la gente no lo usa, quizá el canal deba ser SMS o WhatsApp —que
        el documento no contempla— en vez de correo.

---

## 2. Decisiones de producto

### 2.1 Retirar un equipamiento lo borra de los anuncios ya publicados

Al retirar «Climatisation» del catálogo, el anuncio `YU10025` **dejó de mostrarla**; al
reactivarla, volvió. La fila de enlace no se borra (la ficha filtra por `IsActive`), pero
retirar una entrada cambia lo que dicen los anuncios vivos.

- [x] ✅ **RESUELTO: solo se esconde del formulario.** Los anuncios que ya lo declaraban
      lo siguen mostrando. Se quitó el filtro por `IsActive` de la ficha y del comparador;
      se mantiene al publicar y al editar, para que no pueda elegirse en anuncios nuevos.

### 2.2 El comparador expulsa en silencio en vez de avisar

Con el límite lleno (4), el botón sigue activo, marca «(3/4)» —una menos de las que hay— y
al pulsarlo **entra el nuevo y desaparece uno de los anteriores**, sin decir nada. El doc
§2.9 pide avisar de que está lleno.

- [x] ✅ **RESUELTO: avisar y no dejar añadir** — que ya era lo que hacía.

      ⚠️ **Mi diagnóstico anterior era incorrecto y conviene dejarlo escrito.** El
      comparador **no expulsaba nada al añadir**: `toggle()` devuelve `'full'` y no toca
      la lista, y la pantalla ya mostraba el aviso. Lo que pasaba es que `load()`
      recortaba la selección al **valor de respaldo (3)** al leer de `localStorage`,
      aunque el límite configurado fuera 4. Por eso se veía «(3/4)» con cuatro dentro: se
      perdía uno en **cada recarga**, en silencio.

      Corregido: se lee lo que hay, con un tope de seguridad, y se ajusta al límite real
      en cuanto llega la configuración del servidor.

### 2.3 Invalidar un contrato: qué pasa con el anuncio y la negociación

Invalidar **sí** revierte la reputación (−100 puntos sin borrar el +100, contador de ventas
abajo, QR deja de verificar). Pero:

- [x] ✅ **RESUELTO: invalidar devuelve el anuncio a `Actif`** y borra su fecha de venta.
      La negociación se queda en `Terminée` a propósito: reabrirla es otra decisión.

### 2.4 Se puede cambiar el precio de un anuncio ya vendido

Las acciones «Prix» y «Kilométrage» siguen ofreciéndose sobre un anuncio `Vendu`. No
corrompe nada —el contrato congela el precio acordado—, pero cambia lo que ve quien abre
un anuncio vendido.

- [x] ✅ **RESUELTO: se retiran.** «Prix» y «Kilométrage» ya no se ofrecen sobre un
      anuncio `Vendu`.

### 2.5 El administrador ve los anuncios no públicos en el Marketplace público

La portada le anuncia «49 véhicules disponibles» cuando hay 46: el listado le cuela los
pausados, el vendido y el ocultado. `/vehicles/count` sí devuelve 46, así que la pantalla
se contradice consigo misma.

- [x] ✅ **RESUELTO: el backoffice es el único sitio.** El Marketplace se ve igual desde
      cualquier cuenta. Se conserva el caso del dueño, que sigue viendo sus borradores en
      «Mes annonces» porque ese listado filtra por su propio `sellerId`.

### 2.6 Una sola sesión por cuenta

Pendiente nº 25: entrar desde el móvil expulsa la sesión del ordenador.

- [x] ✅ **RESUELTO: sesiones simultáneas.** El refresh token vivía en una única columna
      de `UserProfile`, así que entrar desde el móvil sobrescribía la del ordenador. Ahora
      hay una tabla `user_refresh_tokens` con una fila por dispositivo, rotación al usarse
      y cierre de sesión que solo cierra la suya. La migración **traslada las sesiones
      abiertas**, así que el despliegue no expulsa a nadie.

### 2.7b El vehículo comprado no entra solo en Mon Garage

Validar el contrato mueve el anuncio a `Vendu`, cierra la negociación, suma la venta
verificada y los puntos — pero **no crea el vehículo en el garaje del comprador**. Lo que
hay es `GET /garage/from-contract/{id}`, que devuelve la ficha precargada para que el
comprador **decida** añadirlo, con `SourceContractId` impidiendo que entre dos veces.

Comprobado el 16/08/2026: tras validar `YC00004`, el garaje del comprador seguía con cero
vehículos. El apartado 3.7 de `flujopruebas.md` lo daba por automático.

- [x] ✅ **RESUELTO: se sigue ofreciendo.** El garaje es del comprador y nadie le mete un
      coche sin pedirlo. Sin cambios de código.

### 2.7 Un anuncio `Réservé` sigue apareciendo en el buscador

Con su etiqueta. Los `Vendu`, `En pause`, `Brouillon`, `Archivé` y los ocultados, no.
Encaja con el doc (§2.2 solo excluye borradores, pausados y archivados) y parece deseable
—lo reservado todavía está en venta—, pero conviene confirmarlo.

- [x] ✅ **CONFIRMADO**: un coche reservado sigue en venta y aparece con su etiqueta.

---

## 3. Correcciones menores, sin decisión de fondo

- [x] ✅ **RESUELTAS LAS TRECE el 16/08/2026** (commit `e5d2331`): el 500 de `/auth/login`
      sin `identifier`, los rechazos que no se explicaban, el filtro «Signalées», el enlace
      al QR desde el backoffice, los enums crudos `Dispute` y `EnExamen`, «Signalement
      clôturé» al ponerlo solo en examen, el buscador de propuestas que ofrecía vendidos,
      las URL en `http://` detrás del proxy de Render, el panel de complétude que no se
      refrescaba, el aviso de error pegado, «Anexer» → «Rattacher», «Jusqu'à trois
      véhicules» escrito a mano, y las pestañas que se rompían tras cada despliegue.

**Lo que quedó fuera de esas trece:**

- [ ] ⚠️ **`credenciales.txt` estuvo versionado en un repositorio público** con la
      contraseña `Test1234!` en claro. Ya está fuera del control de versiones y en
      `.gitignore`, pero **sigue en el historial de git**: hay que darla por quemada.
      No tiene arreglo por código; es cambiar esas contraseñas.
- [x] ✅ **RESUELTO el 16/08/2026: el administrador sí puede descargar el PDF del
      contrato.** El endpoint ya existía; faltaba el botón, y se había dejado aparte
      porque el PDF lleva las pièces d'identité, las direcciones y los teléfonos de las
      dos partes.

      Se sigue el patrón del resto del backoffice: el botón exige **motivo** antes de
      descargar y avisa de que la descarga queda inscrita en la trazabilidad, que es lo
      que hace el endpoint en la misma operación. Se añadieron de paso dos rótulos que
      habrían salido como enum crudo: `ContractDocumentAccessed` y `ReportUnderReview`.

---

## 4. Módulos del producto anterior

- [x] ✅ **RESUELTO el 16/08/2026** (commit `9f57667`): retirados según las 25 decisiones
      marcadas en [`MODULOS-LEGACY.md`](MODULOS-LEGACY.md) — 124 ficheros fuera y 11 tablas
      eliminadas. Con ello desaparecen los importes en **€** de `/precios`, `/transporte` y
      `/financiacion`, y las entradas `Processus`, `Incidents` y `Partenaires` del menú del
      backoffice.

      Verificado en el barrido de enlaces del 16/08/2026 (`flujopruebas.md` §12.11): las 17
      rutas retiradas devuelven 404 y **ningún enlace vivo apunta a ellas**.

---

## 5. Operativo

- [x] ✅ **RESUELTO el 16/08/2026.** `Seed__AdminPhone` apunta ahora a `+221771234500`
      («QA Administration», contraseña conocida). La cuenta anterior volvió a ser usuario
      normal. Con dos cuentas utilizables se pudo probar la Etapa 2 entera.
- [ ] **Retirar los datos de prueba antes de abrir al público.** La lista completa está en
      `flujopruebas.md` §12.9: dos cuentas QA, 48 anuncios sembrados, 6 vendedores, 11
      negociaciones huérfanas, y lo creado en las pruebas (marca `Kia`, modelo `Sportage`,
      equipamiento `SIEGES_CHAUFF`, signalement `SG00001`, demande `YD00001`, anuncios
      `YU10026` y `YU10027` —ambos archivados—, y un vehículo de Mon Garage).
- [ ] **Medir con volumen real.** Todo se midió con 10 usuarios y 49 anuncios, y con la API
      caliente: nada por encima de 350 ms. Eso **no** responde al pendiente nº 19
      (agregaciones de Statistiques en memoria) ni al arranque en frío de Render, que es lo
      que se lleva el primer visitante del día.
- [ ] **Renombrar los servicios a `yoon-u-auto`.** Las URL siguen siendo
      `logistique-les-lions.vercel.app` y `logistique-les-lions-api.onrender.com`, la marca
      anterior. **El renombrado lo tienes que hacer tú** en los paneles de Render y Vercel;
      en cuanto estén los nombres nuevos hay que tocar tres sitios del código:
      `environment.production.ts`, `render.yaml` y el valor por defecto de `Frontend:Url`
      del reseeder. Los namespaces `LogistiqueLesLions.*` se dejan para el final, aparte.
- [ ] **Probar el tiempo real con dos pestañas** (chat, indicador de escritura y acuses de
      lectura). Es lo único de `flujopruebas.md` §12.1 que sigue sin comprobarse; ya es
      posible desde que una cuenta admite sesiones simultáneas.

---

## 6. Encontrado después

- [x] ✅ **RESUELTO**: tras cada despliegue las pestañas abiertas se rompían al pedir un
      `chunk-*.js` del build anterior. Ahora se detecta el fallo de carga y se recarga una
      sola vez (corrección nº 13).

- [ ] ⏸️ **El chat con IA sobre el anuncio queda oculto y aplazado** (decidido el
      16/08/2026). Es `POST /vehicles/{id}/ai/ask`
      (`IAiContentService.AnswerVehicleQuestionAsync`). No es generación de descripciones
      —retirada— ni extracción de documentos —que se conserva porque sí está en el
      documento y sí se usa—. **No aparece en el documento funcional.**

      Estado real comprobado en producción ese día:

      | | |
      |---|---|
      | Pantalla que lo use | **ninguna** — no hay interfaz para él |
      | Autenticación | **ninguna**: el endpoint es anónimo |
      | Clave de Anthropic en Render | **no está puesta** |
      | Qué contesta hoy | el servicio de reserva, **en español**: «el servicio de IA no está configurado. Contacta con un asesor humano», declarando `model: "claude"` |

      Hoy es inerte: sin clave no llama a ningún modelo. **Dos avisos para cuando se
      retome:**
      1. ⚠️ **Antes de poner la clave de Anthropic hay que cerrar el endpoint.** Tal
         como está, sería una llamada a un LLM abierta a cualquiera sin cuenta y sin
         tope de gasto.
      2. La respuesta de reserva está en español y habla de «asesor humano», figura que
         no existe en Yoon u Auto. Se traduce al decidir qué se hace.

- [x] ✅ **RESUELTO el 16/08/2026: opción 1.** El chat de la Etapa 2 no tenía tiempo real
      y había dos pantallas de chat. Se lleva el tiempo real a la negociación —que es
      donde lo pide la especificación— y se retira el buzón duplicado.

      El aviso pasa a salir de **donde se guarda el mensaje**, no del transporte por el
      que llegó la petición: antes solo notificaba el hub desde su propio `SendMessage`,
      y la pantalla de la negociación enviaba por REST, camino que no avisaba a nadie.
      Ahora `SendMessageCommandHandler` persiste la notificación en su transacción y
      empuja después con `IChatPusher` e `INotificationPusher`.

      Con ello **un mensaje nuevo genera por fin notificación en la campana**, que se
      quedaba a cero para siempre.

      `/mensajes` redirige a `/mis-negociaciones`, así que ninguna URL compartida se
      rompe. Quedó un resto menor: `GET /messaging/conversations` ya no lo usa nadie.

- [x] ✅ **RESUELTO**: la página de 404 estaba en español y con el león del producto
      anterior, y quedaban textos sin traducir en el buzón vacío y en las preguntas
      rápidas del chat (una preguntaba por la **exportación**, concepto del producto
      anterior). Corregido el 16/08/2026; ver `flujopruebas.md` §12.11.
