# Migración a Yoon u Auto — Hoja de ruta

Documento de seguimiento de la adaptación de la aplicación (antes *Logistique Les Lions*)
a la especificación funcional **`Yoon u Auto DOC APP.md`** v1.0 (MVP).

## Decisiones de partida

| Decisión | Elección | Motivo |
|---|---|---|
| Módulos fuera del documento | **Se mantienen intactos** de momento | Inventariados en [MODULOS-LEGACY.md](./MODULOS-LEGACY.md); se decide qué hacer con ellos al terminar la adaptación |
| Idioma del frontend | **Francés escrito directamente en las plantillas** | El MVP es monolingüe; i18n de Angular añadiría un build por locale sin beneficio inmediato |
| Namespaces backend | **No se renombran** (`LogistiqueLesLions.*`) | Refactor mecánico y arriesgado; se puede hacer al final en un único commit |
| Moneda | **FCFA (XOF)** | Doc: todos los precios en FCFA |
| Ámbito geográfico | **Senegal** (14 regiones) | Doc: plataforma para Senegal |

## Estado de las partes

Leyenda: ☐ pendiente · ◐ en curso · ☑ completada

### Bloque 0 — Fundamentos
- ☑ **P1** — Identidad, locale francés, moneda FCFA, geografía de Senegal, inventario legacy
- ☑ **P2** — Modelo de usuario y registro (teléfono, Particulier/Professionnel, ciudad, roles)

### Bloque 1 — Etapa 1: «Trouve ta voiture»
- ☑ **P3** — Modelo de datos del vehículo (estado aduanero, ficha técnica, ref. `#YU`, estados, historial de precio)
- ☑ **P4** — Marketplace: tarjetas y ordenación
- ☑ **P5** — Buscador y filtros avanzados + contador de resultados
- ☑ **P6** — Ficha completa del vehículo (galería, bloques, acciones, similares, estado del anuncio)
  *(«Faire une offre» se añade en P14, que aporta la entidad de ofertas)*
- ☑ **P7** — Indicador estadístico de precio (Bonne affaire / Prix correct / Prix élevé)
  *(la pantalla de administración de sus parámetros llega en P34; la tabla ya existe)*
- ☑ **P8** — Mes recherches → Favoris + alertas de bajada de precio
  *(la campana que muestra las notificaciones generadas llega en P12)*
- ☑ **P9** — Mes recherches → Recherches enregistrées + alerta de nuevos vehículos
  *(incluye el botón «Enregistrer la recherche» que quedó pendiente en P5)*
- ☑ **P10** — Mes recherches → Comparateur (hasta 3 vehículos)
  *(la selección persiste en `localStorage`, no en el servidor — ver nota abajo)*
- ☑ **P11** — Mes recherches → Mes demandes «Trouvez-moi une voiture»
  *(lado usuario; la gestión desde el backoffice es P29)*
- ☑ **P12** — Notificaciones globales (campana) — **Bloque 1 completo**

### Bloque 2 — Etapa 2: «Négocie et achète»
- ☑ **P13** — Negociación como entidad + chat asociado al anuncio
- ☑ **P14** — Ofertas, contraofertas y aceptación
- ☑ **P15** — Checklist privada de inspección
- ☑ **P16a** — Contrato: ciclo de vida (crear, corregir, enviar, validar, pedir modificación, anular) y venta verificada
- ☑ **P16b** — Contrato: PDF descargable con QuestPDF y código QR de verificación — **Bloque 2 completo**
  *(«Consultar PDF» y «Verificar QR» desde el backoffice llegan con el bloque de administración)*

### Bloque 3 — Etapa 3: «Mon Garage»
- ☑ **P17** — Mon Garage: alta de vehículos, tarjetas y resumen
  *(el resumen crecerá con «valeur estimée totale» en P21 y «rappels à venir» en P20)*
- ☑ **P18** — Documentos del vehículo (privados, con almacenamiento aparte del público)
  *(elegir qué parte del historial se hace visible al vender es P23)*
- ☑ **P19** — Historial de mantenimiento (Entretien): intervenciones, facturas enlazadas, fotos y agrupación por año
- ☑ **P20** — Recordatorios (fecha / kilometraje) con notificación y trabajo en segundo plano
- ☑ **P21** — Valor estimado y evolución del valor (estadístico, sin IA)
  *(con esto quedan completos el resumen y las tarjetas de Mon Garage que P17 dejó a medias)*
- ☑ **P22** — Complétude du dossier (puntuación por reglas, **nunca diagnóstico mecánico**)
- ☑ **P23** — «Vendre ce véhicule» + transparencia del historial — **Bloque 3 completo**
  *(la edición del borrador y su publicación llegan con «Mes annonces», P24)*

### Bloque 4 — Anuncios y navegación
- ☑ **P24** — Mes annonces (estados, acciones, estadísticas, calidad del anuncio)
  *(cierra el pendiente de que la «Alerte nouveaux véhicules» saltara al crear el borrador
  y no al publicar)*
- ☑ **P25** — Navegación pública vs. personal (4 espacios + menú de avatar) — **Bloque 4 completo**

### Bloque 5 — Administración
- ☑ **P26** — Tableau de bord (usuarios, marketplace, actividad, demanda y Mon Garage)
- ☑ **P27** — Utilisateurs (listado, ficha, estados de cuenta, notas y trazabilidad)
  *(«consultar reportes recibidos» ✅ añadido en P31)*
- ☑ **P28** — Annonces (listado con filtros, ficha administrativa, moderación y petición de corrección)
  *(el filtro «reportado/no reportado» ✅ añadido en P31)*
- ☑ **P29** — Demandes de véhicules (cola de trabajo, responsable, propuestas internas y externas)
- ☑ **P30** — Négociations + Contrats & ventes (acceso justificado al contenido, invalidación administrativa)
  *(los puntos de fidelización llegan con P34)*
- ☑ **P31** — Modération y reportes
  *(cierra tres pendientes: filtro «reportado» en anuncios, reportes en la ficha de usuario y «anuncios pendientes de moderación» en el tableau de bord)*
- ☑ **P32** — Communications (avisos, mantenimiento, información importante y soporte individual)
- ☑ **P33** — Statistiques (usuarios, oferta, demanda, desajuste oferta/demanda y embudo de conversión)
- ☑ **P34** — Configuration: parámetros, feature flags, catálogos, journal d'activité, puntos de fidelización y «Prochainement»

## Desviaciones conscientes respecto al documento

| Parte | Desviación | Motivo |
|---|---|---|
| P10 | La selección del comparador vive en `localStorage`, no en base de datos | El doc dice que «puede conservarse» entre sesiones; `localStorage` lo cumple en el mismo dispositivo. Guardarla en servidor exigiría entidad, migración y sincronización. **Consecuencia**: la selección no viaja entre dispositivos |
| P16a | El contrato tiene un solo estado de espera (`AValider`) donde el documento distingue «Envoyé» y «À valider» | Son el mismo momento visto desde cada parte: para quien lo redacta está enviado, para la otra está por validar. Un solo estado evita transiciones que nadie provoca. **Consecuencia**: la etiqueta que ve el usuario se resuelve en el frontend |
| P16a | No existe «Rejeter» a secas: la otra parte pide una modificación | Ante un error en el contrato es más natural corregirlo que tumbar la operación. Anular sigue siendo posible con «Annuler le contrat» |
| P16a | Validar no exige que haya una oferta aceptada formalmente | Las partes pueden acordar el precio hablando. El importe sugerido sale de la oferta aceptada si la hay, y del precio del anuncio si no |
| P16b | El PDF **no se almacena**: se compone en cada descarga | Los datos del contrato están congelados, así que el documento sale idéntico siempre. La fila en base de datos ya *es* el archivo histórico; guardar el fichero añadiría almacenamiento y un segundo sitio del que se puede perder |
| P16b | La página pública del QR no muestra documentos de identidad, direcciones ni teléfonos | El doc no detalla qué expone. Quien escanea tiene el contrato delante, así que basta con confirmar la venta; publicar la CNI de las partes en una URL sin autenticar sería un problema de protección de datos. Un test bloquea que se cuelen esos campos |
| P16b | El PDF solo se descarga cuando el contrato está validado | El doc genera el PDF *«cuando se valida»*. Un borrador todavía en discusión no es un documento que enseñar |
| P16a/P17 | El vehículo comprado **no entra solo** en Mon Garage: se ofrece con un formulario ya relleno | El documento dice «se incorpora automáticamente». Aquí, al validarse el contrato, el comprador ve «Ajouter ce véhicule à Mon Garage» y el formulario llega precargado del contrato —marca, año, kilometraje, matrícula, fecha y precio—; basta con guardar. Se prefiere a la inserción silenciosa porque Mon Garage es un espacio privado y el dueño debe ver qué entra en él. La regla de «una sola vez» se cumple igual: volver al enlace lleva al vehículo ya creado, no crea otro. **Verificado en producción** |
| P17 | El vehículo de Mon Garage es una entidad propia, no un `Vehicle` con una bandera | Un anuncio es público y tiene precio, estado de publicación y visitas; la ficha del garaje es privada y puede vivir años sin que nadie la vea. Compartir tabla obligaría a filtrar por esa bandera en todas las consultas del Marketplace |
| P17 | El kilometraje solo avanza | Corregirlo a la baja casi siempre es un error de tecleo, y el historial de mantenimiento (P19) y los recordatorios por km (P20) se apoyan en él |
| P18 | Los documentos van a un almacenamiento **privado aparte**, no al de las fotografías | Todo lo subido con `UploadAsync` cae en `uploads/`, que se sirve estáticamente: quien tenga la URL abre el archivo sin autenticarse. Para una carte grise o una CNI eso incumple *«ningún otro usuario podrá acceder a ella»*. Los documentos usan `UploadPrivateAsync` (directorio `private-uploads/`, fuera del estático) y solo se leen por un endpoint que comprueba de quién son. La API **nunca** devuelve la clave del archivo |
| P18 | Borrar un documento conserva la fila pero **borra el archivo de verdad** | El soft delete protege la trazabilidad, pero guardar «por si acaso» un archivo con datos personales que el usuario ha pedido retirar sería lo contrario de lo que pide |
| P32 | El histórico guarda la **audiencia y el número** de destinatarios, no una fila por persona | Las notificaciones ya están en su tabla: duplicar una fila por destinatario multiplicaría el almacenamiento sin añadir información. En la comunicación individual sí consta quién |
| P32 | Las cuentas **bloqueadas** no reciben avisos de plataforma | Un aviso no es para quien ya no puede entrar |
| P32 | El correo solo llega a quien lo tiene, y el envío ocurre **fuera** de la transacción | El correo es opcional en Yoon u Auto, donde la cuenta se identifica por teléfono. Y que un correo falle no puede deshacer un aviso ya guardado, que es el canal principal |
| P31 | «Reportado» significa **tener signalements abiertos**, no haberlos tenido alguna vez | Un reporte ya resuelto no puede dejar un anuncio marcado para siempre |
| P31 | Un usuario no abre dos signalements abiertos sobre lo mismo | Sería ruido en la bandeja, no más información. Cerrado el anterior, sí puede volver a reportar |
| P31 | Cerrar un signalement exige explicar la decisión; pasarlo a examen, no | Al cerrar hay algo que contar a quien lo abrió y que recordar en el equipo. Ponerlo en examen es solo decir «lo estoy mirando» |
| P34 | Los **feature flags son filas**, no columnas de `platform_settings` | Un flag nace para una campaña y se retira dos meses después. Como columna, cada uno costaría una migración |
| P34 | Los puntos son un **libro de movimientos**, con el saldo denormalizado en `UserProfile` | El documento pide consultar «saldo, origen, fecha y movimiento», y eso solo se responde si cada suma dejó su fila. El saldo se guarda aparte para no recorrer el libro al pintar un listado; ambos se escriben en la misma transacción |
| P34 | Invalidar un contrato **compensa** los puntos con un movimiento en negativo | El libro cuenta lo que pasó, incluido lo que se deshizo. Borrar el movimiento original haría desaparecer la venta de la historia |
| P34 | El **código** de un equipamiento no cambia una vez creado | Los anuncios ya enlazados dejarían de significar lo mismo. El nombre visible sí se puede corregir |
| P34 | Retirar del catálogo **no borra** | Una marca usada por doscientos anuncios no puede desaparecer. `IsActive` la esconde de los formularios y deja los anuncios intactos |
| P34 | «Prochainement» cuelga del **perfil**, no del menú principal | El documento lo dice literalmente: «no debemos llenar el menú con algo que todavía no existe» |
| P34 | Retirar un «Ça m'intéresse» es **soft delete** con índice único parcial | La regla del proyecto prohíbe el borrado físico. El índice parcial permite volver a declarar el interés después de retirarlo |
| P35 | La portada **no cita testimonios** | Los del producto anterior eran inventados. Una plataforma que no tiene todavía clientes no puede citarlos |
| P35 | La portada **no muestra recuentos** por carrocería | Dependen de lo publicado en cada momento; una cifra fija es una promesa que la búsqueda no cumple |
| P33 | **«Vehículos más comparados» no se mide** | La selección del comparador vive en `localStorage` (desviación de P10): el servidor nunca ve qué se compara. Medirlo exigiría persistir la selección o enviar eventos de uso, que es otra decisión de producto. La pantalla lo dice en su pie |
| P33 | Los precios se muestran por su **mediana**, con la media al lado en letra pequeña | Un puñado de anuncios muy caros desplaza la media y da una idea falsa del mercado. La media se conserva porque el contraste entre las dos cifras es en sí mismo informativo |
| P33 | «Activo» significa **haberse conectado** en el periodo | Es lo único que la aplicación registra hoy sobre actividad (`LastLoginAt`). Medir actividad real exigiría instrumentar la navegación |
| P33 | El desajuste cuenta **personas**, no búsquedas | Tres búsquedas guardadas del mismo modelo por la misma persona son una persona interesada, no tres. Si no, cualquiera podría inflar la demanda |
| P33 | Las agregaciones se hacen **en memoria**, no en SQL | El proveedor en memoria de los tests no traduce `GroupBy` con proyección, y los filtros guardados son JSON que habría que interpretar con funciones propias de PostgreSQL. **Consecuencia**: revisar el coste cuando el volumen crezca — anotado en `PENDIENTES-TECNICOS.md` |
| P33 | Las **vistas del embudo son acumuladas**, el resto del periodo | `Vehicle.ViewsCount` es un contador, no un histórico: el anuncio no guarda cuándo se vio cada vez. La pantalla lo advierte bajo el título |
| P31 | Suspender o bloquear se hace desde **Utilisateurs**, no desde la moderación | Son medidas sobre la cuenta y ya viven en su pantalla, con su registro. Desde el signalement se advierte y se decide |
| P30 | Leer una conversación privada y registrarlo son **la misma operación** | Si fueran dos pasos, el registro dependería de que alguien se acordara. Y la ficha estructural no trae los mensajes: abrirla no basta para leerlos |
| P30 | El motivo enumerado no basta: hace falta explicar **por qué esta conversación** | «Litige» sin más no dice nada cuando dentro de un año alguien pregunta qué pasó |
| P30 | Invalidar una venta verificada **descuenta** la venta del vendedor | La reputación no puede sostenerse sobre un contrato invalidado. El anuncio y la negociación no se tocan: son de sus dueños |
| P30 | No existe ningún comando administrativo que valide un contrato | La especificación lo prohíbe expresamente. Un test comprueba que el único comando sobre contratos es el de invalidar |
| P29 | Una solicitud terminada o anulada **no se reabre** | Si el usuario vuelve a necesitar un coche, crea otra. Así la anterior conserva íntegro lo que se hizo por ella |
| P29 | Solo se propone un anuncio que el usuario **pueda abrir** | Proponer un borrador, un vendido o uno ocultado por moderación sería mandarle a una puerta cerrada |
| P29 | Proponer un vehículo mueve la solicitud a «Véhicule proposé» sola | Es exactamente lo que el usuario estaba esperando; obligar a cambiar el estado a mano solo añade un olvido posible |
| P29 | Los costes adicionales van **aparte** del precio en la propuesta externa | Quien pide un coche importado necesita ver qué es el vehículo y qué es traerlo |
| P28 | «Ocultar» usa una marca propia (`AdminHiddenAt`), no el estado `EnPause` | `EnPause` es una decisión de quien publica: si la moderación usara el mismo estado, el vendedor levantaría la medida él mismo con un clic. Ahora el anuncio desaparece del Marketplace aunque su estado siga siendo «Actif», y solo un administrador lo repone |
| P28 | El administrador **no edita** marca, kilómetros ni precio | Lo dice el documento: la información comercial pertenece a quien publica. La vía es «demander une correction», que avisa al vendedor y deja constancia de que se le avisó |
| P28 | Ocultar, archivar y eliminar exigen motivo; marcar para revisión, no | Las tres primeras cambian lo que el usuario ve y tendrá que poder leerse cuando reclame. La marca de revisión es una señal interna que no le afecta |
| P27 | Restringir una cuenta **exige motivo escrito**; reactivarla, no | Es lo que se lee en el histórico cuando alguien pregunta qué pasó. Devolver una cuenta a la normalidad no necesita justificarse |
| P27 | Suspender exige **fecha de final**; bloquear no la admite | Una suspensión sin final es un bloqueo con otro nombre. Con fecha, la cuenta vuelve sola: no depende de que alguien se acuerde de levantarla |
| P27 | Un administrador no puede actuar sobre sí mismo ni sobre otro administrador | Lo primero dejaría la plataforma sin quien la gestione, sin forma de deshacerlo desde dentro. La gestión de administradores no se hace desde esta pantalla |
| P27 | El registro de acciones (`admin_actions`) es **append-only**; las notas sí se retiran | Una decisión con su motivo es historia y no se reescribe. Una nota es contexto de trabajo, y cada quien retira las suyas |
| P27 | De Mon Garage solo se muestra **cuántos** vehículos hay | Su documentación e historial son privados; el backoffice no debe ser una puerta trasera a ellos |
| P26 | «Anuncios pendientes de moderación» se añadió en **P31**, cuando ya había reportes | Mostrar antes un contador permanentemente a cero habría sido peor que no mostrarlo |
| P26 | «Ventas verificadas» se cuenta como contratos validados, no por otro camino | Son lo mismo por definición. Contarlo dos veces permitiría que las dos cifras discrepasen en pantalla |
| P26 | «Nuevos anuncios» cuenta publicaciones, no borradores creados | Un borrador no lo ha visto nadie: no es actividad del marketplace |
| P25 | Los módulos legacy **salen del menú principal** | El documento fija la navegación pública en tres entradas: Voitures · Vendre · Trouvez-moi une voiture. Tramitación, concesionarios y compañía siguen accesibles por URL y desde el pie; su destino se decide en **P35**, y devolverlos al menú es una línea |
| P25 | Fuera el selector de idioma (ES/EN/FR/DE) | No traducía nada: solo guardaba una letra en `localStorage`. La aplicación es en francés |
| P25 | «Mes recherches» es una **página con cuatro accesos**, no cuatro pestañas | Favoris, Recherches enregistrées, Comparateur y Mes demandes ya son cuatro pantallas completas y con URL propia. Reunirlas en pestañas obligaba a rehacerlas para ganar poco; la portada añade contadores y avisos, que es lo que faltaba |
| P25 | El panel al que se llega tras identificarse pasa a ser la **puerta de los cuatro espacios** | Antes repetía accesos sueltos en español. Ahora resume lo que hay en marcha en cada espacio y lo que reclama atención |
| P24 | Un anuncio **vendido no vuelve atrás**: solo puede archivarse | Su ficha sostiene contratos, favoritos y comparaciones; reabrirlo cambiaría el pasado. Para volver a venderlo, se duplica |
| P24 | No se publica un anuncio sin precio | Es lo que más frena a quien busca, y sin precio no entra en filtros ni comparaciones |
| P24 | «Dupliquer» no copia el VIN ni los contadores | El VIN identifica a **un** coche concreto; las visitas y los favoritos son del anuncio original |
| P24 | La calidad del anuncio es otra cosa que la complétude del garaje | Aquella mide el historial del vehículo; esta, lo bien presentado que está el anuncio de cara a quien lo mira |
| P24 | Reordenar fotos exige el listado **completo** | Un orden parcial dejaría posiciones repetidas y el resultado dependería de cómo las ordenase la base de datos |
| P23 | El borrador se crea **sin precio**, aunque haya valor estimado | El documento pide revisar expresamente el precio de venta. Poner la estimación dentro del anuncio sería poner en boca de quien vende una cifra que no ha decidido. La estimación se ofrece como sugerencia **en pantalla** |
| P23 | Tampoco se heredan estado aduanero ni descripción | Son las otras dos cosas que el documento marca para revisar, y el garaje no las guarda |
| P23 | El equipamiento no se precarga | Mon Garage no registra equipamiento (P17 no lo incluyó). Si se añade a la ficha del garaje, aquí solo habría que copiarlo |
| P23 | Las fotografías se **reutilizan** por URL, no se duplican | Las del garaje ya están en el almacenamiento público justo por esto: la venta era su destino previsible |
| P23 | Compartir una intervención **no** comparte su factura | Enseñar que se hizo una revisión no obliga a enseñar un papel que puede llevar datos personales. Son dos casillas |
| P22 | Se llama **«Complétude du dossier»**, no «Santé du véhicule» | El documento admite las dos y pide explicar qué significa. «Santé» invita a leerlo como estado mecánico, que es justo lo que la regla fundamental prohíbe. El aviso va bajo el indicador, en la propia pantalla |
| P22 | Los pesos de la puntuación viven en el código, no en base de datos | A diferencia de los márgenes del indicador de precio —que el documento pide configurables— estos pesos *son* la definición del indicador. Están juntos y a la vista al principio de `CompletenessCalculator` |
| P22 | No tener rappels **no penaliza**; lo que resta es tenerlos vencidos | No haber programado avisos no es descuidar el vehículo |
| P22 | Sin historial de mantenimiento, el apartado de facturas no penaliza | Ya penaliza el apartado del historial; descontar dos veces por lo mismo daría un porcentaje injustamente bajo |
| P21 | La muestra se busca por **niveles**, soltando criterios hasta reunir el mínimo | El documento pide usar la versión «cuando haya suficiente información», así que admite exigir menos cuando no la hay. Se empieza por lo más parecido (mismo uso, misma mecánica, misma región) y se afloja; **nunca se baja de marca, modelo y franja de años**. Si ni así hay muestra, no se muestra cifra |
| P21 | La ubicación sale de la **región del dueño**, no de la ficha del garaje | La ficha de Mon Garage no guarda ubicación: el vehículo está donde vive su dueño, que es donde se vendería |
| P21 | Los anuncios **vendidos** también entran en la muestra | Lo que alguien pagó de verdad dice tanto sobre el valor como lo que otro pide hoy. Los borradores no entran: no son mercado |
| P21 | El historial de valor se guarda al **consultarlo**, no con un proceso periódico | La evolución se construye sola y solo para los vehículos que alguien mira. Un proceso que recorriera todos los garajes cada mes gastaría lo mismo para llenar de puntos fichas que nadie abre |
| P20 | El paso `À venir → À faire` lo decide **solo el sistema**; el usuario no puede forzarlo | Es el estado que significa «la condición se ha cumplido». Si el usuario pudiera ponerlo a mano dejaría de significar eso |
| P20 | Los rappels por fecha los evalúa un trabajo en segundo plano cada 6 h; los de kilometraje, el propio acto de declarar el kilometraje | La fecha se cumple sola con el paso del tiempo; el kilometraje no avanza si el usuario no lo declara, y el documento **prohíbe estimarlo** |
| P20 | Un rappel avisa **una sola vez** (`NotifiedAt`) | Con varias instancias del servidor el trabajo en segundo plano se ejecuta en todas; sin esa marca, el usuario recibiría el aviso repetido |
| P19 | La factura de una intervención se **enlaza** a un documento de Documents, no se sube aparte | El papel vive en un único sitio: la ficha muestra «Facture disponible ✓» sin duplicar archivos, y al reclasificar el documento la intervención sigue apuntando al mismo |
| P19 | Registrar una intervención con kilometraje mayor pone al día el del vehículo | Es la lectura más reciente que tenemos. Una intervención antigua no lo hace retroceder |
| P19 | Las fotos de una intervención van al almacenamiento **privado**, las del vehículo al público | Las del vehículo acabarán reutilizándose al publicar el anuncio (P23); las de una intervención no están destinadas a hacerse públicas nunca |
| P18 | Reclasificar un documento no sustituye el archivo | Cambiar tipo, nombre, fecha u observaciones es corregir metadatos. Para cambiar el archivo se sube otro y se borra el anterior, y así el histórico no cambia de contenido a espaldas de nadie |
| P17 | El enlace a Mon Garage se ha añadido al menú actual sin rehacerlo | La navegación con los cuatro espacios es P25. Sin el enlace la pantalla solo sería accesible escribiendo la URL |

### Bloque 6 — Cierre
- ◐ **P35** — Módulos legacy y portada
  - ☑ Portada, buscador, «comment ça marche» y pie reescritos en francés para Senegal
  - ☑ Los 30 bloques del producto anterior, inventariados en `flujopruebas.md` §9
  - ☐ **Decisión pendiente del usuario**, bloque a bloque, tras probar en producción
