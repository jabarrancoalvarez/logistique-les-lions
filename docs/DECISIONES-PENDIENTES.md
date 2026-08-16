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
- [ ] ⏸️ **APLAZADO — estamos en fase de demo de pruebas** (decidido el 16/08/2026). No se
      toca nada de lo legal por ahora. Rellenar razón social, domicilio, RCCM y NINEA
      reales, y que lo revise un abogado en Senegal, se hará antes de abrir al público.

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

- [x] ✅ **Disco de Render efímero** (pendiente nº 2) — **RESUELTO para la demo el
      16/08/2026: los archivos se guardan en Neon.** El disco de Render se recrea en cada
      despliegue, así que lo que subían los usuarios (fotos de anuncio, documentos de Mon
      Garage) se perdía. Para la demo, con pocos ejemplos, se guardan los bytes en la
      propia base: una tabla `stored_files` y un tercer proveedor de `IStorageService`,
      `DatabaseStorageService`, activado con **`Storage:Provider=database`** (ya puesto en
      `render.yaml`). Cero servicios externos, cero claves, y sobreviven a los despliegues.
      Las fotos públicas se sirven por `GET /files/{clave}`.

      ⚠️ **No es la solución para un catálogo grande**: comparte el medio giga con los
      datos y sirve las imágenes más despacio que un CDN. Cuando crezca, se pasa a
      Cloudflare R2 —`ObjectStorageService` ya está escrito— cambiando el proveedor a `r2`
      y rellenando el bucket y las cinco claves. El resto de la app no cambia, porque todo
      pasa por `IStorageService`. Pasos de R2, para ese día:

      1. Crear un bucket en Cloudflare R2.
      2. Exponer **solo el prefijo `public/`**. ⚠️ Si se abre el bucket entero, la
         documentación privada de Mon Garage queda al alcance de cualquiera.
      3. Token de API con lectura y escritura sobre el bucket.
      4. En Render → Environment: `Storage__Provider=r2`, `Storage__Bucket`,
         `Storage__ServiceUrl`, `Storage__AccessKey`, `Storage__SecretKey`,
         `Storage__PublicBaseUrl`.

      ⚠️ Si `Storage__Provider` está definido **a mano en el panel de Render**, manda el
      panel sobre `render.yaml`: hay que ponerlo a `database` allí también.
- [ ] ⏸️ **Correo — APLAZADO, esperando datos** (pendiente nº 3; confirmado el 16/08/2026:
      el dominio propio para el correo lo tienen que pasar, así que está parado). Sin
      `Email__ApiKey` no sale ningún correo; las comunicaciones del backoffice solo llegan
      como notificación interna. ⚠️ Desde un Gmail no se puede enviar: hace falta dominio
      propio para el remitente.

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

- [x] ✅ **CERRADO el 16/08/2026: sin acción necesaria.** `credenciales.txt` estuvo
      versionado con la contraseña `Test1234!` en claro y sigue en el historial de git,
      pero una auditoría del historial completo confirma que **esa contraseña ya no abre
      ninguna cuenta**:
      - El sembrador actual usa `UnusablePassword()` —hash de dos GUID aleatorios—;
        `Test1234!` no aparece en el código.
      - Las cuentas vivas usan `YoonQA2026!`.
      - Lo filtrado eran correos del producto anterior con roles (`Dealer`, `Seller`,
        `Buyer`, `Moderator`) que ya no existen.

      La auditoría no encontró **ningún otro secreto real** en el historial: lo demás son
      valores de laboratorio de `docker-compose` (Postgres local y una clave JWT de
      ejemplo) que no dan acceso a producción, donde las claves vienen del entorno y
      `appsettings.json` no lleva ninguna.

      ✅ **Comprobado el 16/08/2026: la `Jwt__Key` de producción es un secreto propio**, no
      la pública de `docker-compose`. Producción firma sus tokens con su propia clave, así
      que nadie puede forjarse un token de administrador con la del repositorio.

      ℹ️ Recomendación permanente: `Jwt__Key` nunca debe salir de Render. Si en algún
      momento se sospecha que se ha expuesto, se rota (otra cadena aleatoria de 32+
      caracteres): el único efecto es que las sesiones abiertas caducan y hay que volver a
      entrar.
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
- [ ] 🔴 **El arranque en frío de Render es de 47 segundos. Es una decisión de negocio.**
      Medido el 16/08/2026 dejando la API sin una sola petición durante 17 minutos:

      | Petición | En frío | En caliente |
      |---|---|---|
      | `health/live` | **47,4 s** | 127 ms |
      | siguiente `health/live` | 0,2 s | — |
      | `vehicles/count` | 1,0 s | 126 ms |
      | listado de 12 anuncios | 2,9 s | 181 ms |

      El plan gratuito **suspende el servicio tras ~15 minutos sin tráfico**. Quien llega
      el primero se encuentra la página cargada —eso lo sirve Vercel al instante— y el
      catálogo vacío durante casi un minuto. En un país donde mucha gente entra desde el
      móvil con datos, eso es un abandono, no una espera.

      Ojo: no es solo el contenedor. La API **aplica las migraciones al arrancar** y Neon
      también suspende su cómputo, lo que explica que las dos primeras consultas de datos
      sigan tardando 1 y 2,9 segundos con el servicio ya despierto.

      ⏸️ **APLAZADO para la demo** (decidido el 16/08/2026): no se cambia el plan de Render
      porque esto es una demo. Se decidirá el servidor real de producción más adelante, y
      entonces se elige entre estas opciones:
      1. **Plan de pago en Render** (~7 $/mes). Quita la suspensión. Es la única que
         resuelve el problema de verdad.
      2. **Mantenerlo despierto con un ping** cada 10 minutos. El plan gratuito da 750
         horas al mes y un servicio continuo consume ~744, así que cabe justo. Frágil: si
         el ping falla una noche, el primer visitante paga los 47 segundos igual.
      3. **Otro proveedor.** Al elegir el alojamiento real de producción se resuelve de
         raíz.
- [x] ✅ **Pendiente nº 19 resuelto el 16/08/2026: las estadísticas agregan en SQL.**
      Cargaban en memoria todos los anuncios activos para calcular medias, medianas y
      rankings. Ahora es `GROUP BY` con `LIMIT`, y la mediana se resuelve contando y
      pidiendo solo las filas centrales.
- [ ] **Medir con volumen real** sigue pendiente en lo demás. Todo se ha medido con 10
      usuarios y 49 anuncios: los tiempos en caliente no dicen nada sobre cómo se comporta
      con un catálogo grande.
- [ ] **El indicador de precio se trae a memoria todos los anuncios activos de las marcas
      que aparezcan en la página**, y filtra por modelo y año en C#
      (`PriceIndicatorService.CalculateManyAsync`). El viaje único a la base está bien
      pensado —evita N+1—, pero el cubo sobra: se puede acotar además por los modelos
      concretos de la página y la franja de años, que es justo lo que luego se filtra.
      Ocurre en **cada carga del listado público**, no en el backoffice. ⚠️ Cuidado con
      los anuncios sin modelo, que hoy se comparan entre sí.
- [x] ✅ **RESUELTO el 16/08/2026: el dominio público ya es `yoon-u-auto.vercel.app`.**
      Verificado de punta a punta sobre él: catálogo, fotos, login, llamada autenticada,
      backoffice y SignalR.

      ⚠️ **Lo que aprendimos, para no repetirlo:** ni Vercel ni Render cambian el
      hostname al renombrar. En Vercel hubo que **añadir el dominio** en *Settings →
      Domains* y retirar el antiguo. En Render **no hay forma** sin recrear el servicio y
      migrar a mano las diez variables de entorno, así que la API se queda en
      `logistique-les-lions-api.onrender.com` — razonado en `render.yaml`. No la ve
      nadie: solo la llama el frontend. Con un dominio propio se resuelve añadiendo un
      dominio personalizado.

      Dos cosas que el cambio de dominio rompió y conviene tener presentes para el
      siguiente:
      1. **CORS.** `Cors__AllowedOrigins` seguía autorizando solo el dominio viejo y la
         web quedó servida pero muda. Se arregla en Render, sin desplegar nada. Acepta
         lista separada por comas, así que **autoriza el dominio nuevo antes de crearlo**.
      2. **Las fotos del catálogo**, que guardaban la URL absoluta del frontend dentro de
         la base de datos. Corregido de raíz: ahora son relativas.

      Queda pendiente y es opcional: `Jwt__Issuer` y `Jwt__Audience` están renombrados en
      `render.yaml` pero **mandan las variables del panel de Render**, así que los tokens
      se siguen emitiendo como `logistique-les-lions-*`. Cambiarlas allí es seguro.

      Los namespaces `LogistiqueLesLions.*` y el nombre del repositorio en GitHub siguen
      sin tocar, a propósito.
- [ ] **Probar el tiempo real con dos pestañas** (chat, indicador de escritura y acuses de
      lectura). Es lo único de `flujopruebas.md` §12.1 que sigue sin comprobarse; ya es
      posible desde que una cuenta admite sesiones simultáneas.

---

## 6. Encontrado después

- [x] ✅ **RESUELTO**: tras cada despliegue las pestañas abiertas se rompían al pedir un
      `chunk-*.js` del build anterior. Ahora se detecta el fallo de carga y se recarga una
      sola vez (corrección nº 13).

- [ ] ⏸️ **El chat con IA sobre el anuncio queda oculto y aplazado a una fase mucho más
      avanzada de la app** (decidido el 16/08/2026). Es `POST /vehicles/{id}/ai/ask`
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
