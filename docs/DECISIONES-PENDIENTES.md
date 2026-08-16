# Decisiones pendientes

Todo lo que ha salido probando contra producción y **necesita que decidas tú**, no que se
programe sin más. Ordenado por lo que bloquea abrir al público.

- Lo encontrado y ya corregido está en [`../flujopruebas.md`](../flujopruebas.md) §11 y §12.
- Los módulos del producto anterior, uno a uno, en [`MODULOS-LEGACY.md`](MODULOS-LEGACY.md).
- La deuda técnica de fondo, en [`PENDIENTES-TECNICOS.md`](PENDIENTES-TECNICOS.md).

> Marca la casilla cuando la decisión esté tomada y anota al lado lo que se hace.

---

## 0. Antes que nada: dos correcciones sin desplegar

No son decisiones, son trabajo hecho esperando salida. Mientras no se desplieguen, la
aplicación tiene un agujero abierto.

- [ ] **Desplegar la corrección de visibilidad de anuncios.** Hoy, en producción, un
      anuncio en `Brouillon`, `EnPause`, `Archive` **o ocultado por moderación** se sirve
      entero a cualquiera que tenga el enlace. Ocultar por moderación no oculta nada.
- [ ] **Desplegar la corrección del listado de negociaciones** (decía «12» y enseñaba 1).

Ambas con pruebas: 511 en verde y sin cambios de modelo pendientes.

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

- [ ] **Decisión:** ¿con qué datos reales se reescriben (razón social, domicilio, registro,
      identificador fiscal) y bajo qué normativa senegalesa? ¿Hace falta un asesor legal
      local, o hay textos ya redactados?
- [ ] Mientras tanto, ¿se retiran del pie o se dejan visibles?

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

- [ ] **Decisión:** ¿retirar debe (a) esconderlo solo del formulario de publicación,
      dejando intactos los anuncios que ya lo declaraban, o (b) desaparecer de todas
      partes, como ahora?

  *Recomendación: la (a).* Un anuncio publicado describe un coche real; que el coche deje
  de tener aire acondicionado porque un administrador tocó el catálogo es raro.

### 2.2 El comparador expulsa en silencio en vez de avisar

Con el límite lleno (4), el botón sigue activo, marca «(3/4)» —una menos de las que hay— y
al pulsarlo **entra el nuevo y desaparece uno de los anteriores**, sin decir nada. El doc
§2.9 pide avisar de que está lleno.

- [ ] **Decisión:** ¿avisar y no dejar añadir (lo que pide el doc), o dejar la sustitución
      pero contándola («se ha retirado X»)? En cualquier caso hay que corregir el contador.

### 2.3 Invalidar un contrato: qué pasa con el anuncio y la negociación

Invalidar **sí** revierte la reputación (−100 puntos sin borrar el +100, contador de ventas
abajo, QR deja de verificar). Pero:

- [ ] El **anuncio se queda en `Vendu`**. El vendedor lo recupera por un camino poco
      evidente: Archiver → Remettre en brouillon → publicar. ¿Debería invalidar devolverlo
      a `Actif`, u ofrecer «Remettre en vente» desde `Vendu`?
- [ ] La **negociación se queda en `Terminée`**. Si se invalidó por fraude quizá deba
      reabrirse; si fue un error administrativo, quizá no.

### 2.4 Se puede cambiar el precio de un anuncio ya vendido

Las acciones «Prix» y «Kilométrage» siguen ofreciéndose sobre un anuncio `Vendu`. No
corrompe nada —el contrato congela el precio acordado—, pero cambia lo que ve quien abre
un anuncio vendido.

- [ ] **Decisión:** ¿se retiran esas dos acciones cuando el anuncio está `Vendu`?

### 2.5 El administrador ve los anuncios no públicos en el Marketplace público

La portada le anuncia «49 véhicules disponibles» cuando hay 46: el listado le cuela los
pausados, el vendido y el ocultado. `/vehicles/count` sí devuelve 46, así que la pantalla
se contradice consigo misma.

- [ ] **Decisión:** ¿el backoffice es el único sitio donde se ven los no públicos, o se
      mantiene el atajo para el administrador?

### 2.6 Una sola sesión por cuenta

Pendiente nº 25: entrar desde el móvil expulsa la sesión del ordenador.

- [ ] **Decisión:** ¿se acepta, o se permiten sesiones simultáneas?

### 2.7b El vehículo comprado no entra solo en Mon Garage

Validar el contrato mueve el anuncio a `Vendu`, cierra la negociación, suma la venta
verificada y los puntos — pero **no crea el vehículo en el garaje del comprador**. Lo que
hay es `GET /garage/from-contract/{id}`, que devuelve la ficha precargada para que el
comprador **decida** añadirlo, con `SourceContractId` impidiendo que entre dos veces.

Comprobado el 16/08/2026: tras validar `YC00004`, el garaje del comprador seguía con cero
vehículos. El apartado 3.7 de `flujopruebas.md` lo daba por automático.

- [ ] **Decisión:** ¿se añade solo al validar, o se sigue ofreciendo al comprador?
      *Sin recomendación fuerte:* automático es más cómodo, pero mete un coche en el
      garaje de alguien sin que lo pida, y el garaje es suyo.

### 2.7 Un anuncio `Réservé` sigue apareciendo en el buscador

Con su etiqueta. Los `Vendu`, `En pause`, `Brouillon`, `Archivé` y los ocultados, no.
Encaja con el doc (§2.2 solo excluye borradores, pausados y archivados) y parece deseable
—lo reservado todavía está en venta—, pero conviene confirmarlo.

- [ ] **Decisión:** ¿se confirma este comportamiento?

---

## 3. Correcciones menores, sin decisión de fondo

Si estás de acuerdo, se hacen y ya. Están aquí para que las veas, no para debatirlas.

- [ ] **Los rechazos no se explican.** Suspender sin motivo, leer una conversación sin
      justificarla, ajustar puntos sin motivo o bajar el kilometraje **no hacen nada y no
      dicen por qué**: el botón sigue activo y no aparece mensaje. La API sí devuelve el
      error (`Admin.ReasonRequired`, `GarageVehicle.MileageWentBackwards`).
      ⚠️ **El patrón correcto ya existe**: publicar un borrador sin precio responde
      «Publication impossible. Vérifiez que l'annonce a un prix.» Hay que replicarlo.
- [ ] 🔴 **`POST /auth/login` devuelve 500 si el cuerpo no trae `identifier`.** Con la
      contraseña equivocada responde 401, que es lo correcto; pero si falta el campo,
      revienta. Es un endpoint **anónimo y con límite de peticiones**, o sea el primero
      que va a recibir basura desde fuera. Debería contestar 400.
- [ ] ⚠️ **`credenciales.txt` estaba versionado en un repositorio público** con la
      contraseña `Test1234!` en claro. Ya está fuera del control de versiones y en
      `.gitignore`, pero **sigue en el historial de git**: hay que darla por quemada.
- [ ] **Falta el filtro «reportadas»** en Annonces. La API acepta `Reported`; el formulario
      solo expone «Masquées» y «À réviser». El doc §6.4 lo pide.
- [ ] **No se puede consultar el PDF del contrato ni verificar el QR desde el backoffice.**
      La ficha enseña el código de verificación como texto, pero no hay enlace ni descarga.
      El doc §6.7 pide ambas cosas.
- [ ] **Enums crudos en francés a medias**: el journal escribe `Dispute` en vez de «Litige
      entre les parties»; el historial de un signalement escribe `EnExamen`. Mismo fallo
      que se corrigió en la ficha de negociación (commit 239cca6).
- [ ] **El historial de un signalement rotula «Signalement clôturé» también al ponerlo en
      examen**, que no lo cierra.
- [ ] **Al proponer un vehículo interno, el buscador ofrece anuncios vendidos** (`YU10025`,
      en `Vendu`, aparece entre las propuestas posibles).
- [ ] **La API devuelve las URLs de fichero en `http://`**, no `https://`. Chrome deja
      avisos de «Mixed Content» y las eleva él solo.
- [ ] **El panel de complétude no se refresca**: al cerrar un recordatorio sigue diciendo
      «2 rappels en retard» hasta recargar.
- [ ] **El aviso de error de catálogo se queda pegado** en pantalla aunque la acción
      siguiente funcione.
- [ ] **Falta de ortografía**: «**Anexer** une annonce Yoon u Auto» → *Annexer*.
- [ ] **«Jusqu'à trois véhicules» está escrito a mano** en `/mis-busquedas`, con el límite
      configurado en 4. El resto de la aplicación lee el ajuste.

---

## 4. Módulos del producto anterior

Todo eso vive en [`MODULOS-LEGACY.md`](MODULOS-LEGACY.md), módulo a módulo y con casilla
para marcar **eliminar / adaptar / conservar**. Lo urgente de allí:

- [ ] **Quedan euros en producción**: `/precios`, `/transporte` y `/financiacion` muestran
      importes en **€**. Solo se llega por URL directa, pero están vivas y contradicen
      «solo FCFA».
- [ ] **El menú del backoffice sigue enseñando `Processus`, `Incidents` y `Partenaires`**,
      en español y con datos europeos («Gestoría Iberia», «Carfax Europe Inspectors»).
      Es lo primero que ve un administrador.

---

## 5. Operativo

- [ ] **La contraseña de `+221770000101` no consta en ninguna parte**, y esa cuenta es el
      administrador actual (`Seed:AdminPhone`). Bloquea probar la Etapa 2 y el tiempo real,
      que exigen dos cuentas a la vez. Opciones: recuperarla, apuntar `Seed:AdminPhone` a
      una cuenta nueva con contraseña conocida, o dar el backoffice por probado.
- [ ] **Retirar los datos de prueba antes de abrir al público.** La lista completa está en
      `flujopruebas.md` §12.9: dos cuentas QA, 48 anuncios sembrados, 6 vendedores, 11
      negociaciones huérfanas, y lo creado en las pruebas (marca `Kia`, modelo `Sportage`,
      equipamiento `SIEGES_CHAUFF`, signalement `SG00001`, demande `YD00001`, anuncios
      `YU10026` y `YU10027` —ambos archivados—, y un vehículo de Mon Garage).
- [ ] **Medir con volumen real.** Todo se midió con 10 usuarios y 49 anuncios, y con la API
      caliente: nada por encima de 350 ms. Eso **no** responde al pendiente nº 19
      (agregaciones de Statistiques en memoria) ni al arranque en frío de Render, que es lo
      que se lleva el primer visitante del día.

---

## 6. Encontrado después

- [ ] ⚠️ **Tras cada despliegue, las pestañas abiertas se rompen.** Angular carga las
      pantallas por trozos (`chunk-*.js`) y, al desplegar, los del build anterior dejan de
      existir: la pestañá abierta pide uno, recibe HTML y falla con
      «Failed to fetch dynamically imported module». El usuario ve una pantalla rota y
      solo lo arregla recargando a mano.
      Se corrige capturando ese fallo de carga y recargando la página una vez.
      Visto en producción el 16/08/2026 tras cinco despliegues seguidos.
