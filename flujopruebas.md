# Yoon u Auto — Plan de pruebas

Lista exhaustiva para validar en **producción** todo lo implementado en la migración
(partes P1–P35), comparándolo punto por punto con `Yoon u Auto DOC APP.md`.

> **Cómo usarla.** Cada bloque cita la sección del documento funcional que valida. Marca
> la casilla cuando lo hayas comprobado **con los ojos**, no por deducción. Lo que falle
> se anota al margen; al final del documento hay un apartado para las decisiones
> pendientes.

**Estado de partida:** ninguna de las 25 migraciones nuevas se ha ejecutado nunca contra
PostgreSQL. La aplicación no se ha arrancado en local durante toda la adaptación. Es la
primera vez que este código toca una base de datos real.

---

## 0. Despliegue y arranque

### 0.1 Antes de nada

- [ ] **Copia de seguridad de Neon** hecha y verificada. Las migraciones eliminan nueve
      columnas del producto anterior (`first_name`, `last_name`, `company_name`,
      `company_vat`, `country_code`, `is_active` en `user_profiles`; `description_en`,
      `specs`, `features` en `vehicles`). Los datos se copian antes de borrarlas, pero
      sin copia de seguridad no hay vuelta atrás.
- [ ] Anotar cuántas filas hay hoy en `user_profiles`, `vehicles` y `conversations`,
      para compararlo después.

### 0.2 Migraciones

La API las aplica **sola al arrancar** (`db.Database.MigrateAsync()` en `Program.cs`).
No hay que ejecutarlas a mano: basta con desplegar. Si una falla, la API **no arranca**
y Render marca el despliegue como fallido.

Son 31 en total, 25 de ellas nuevas, y se aplican en este orden:

```
DeviseFcfaParDefaut · ModeleUtilisateurYoonUAuto · ModeleVehiculeYoonUAuto
IndicateurDePrix · AlertesFavoris · RecherchesEnregistrees · DemandesDeVehicules
Negociations · Offres · InspectionPrivee · Contrats · CodeVerificationContrat
MonGarage · DocumentsDuGarage · HistoriqueEntretien · RappelsDuGarage
EstimationDeValeur · CompletudeDuDossier · VendreCeVehicule
GestionDesUtilisateurs · ModerationDesAnnonces · DemandesAdministration
Moderation · Communications · ConfigurationEtPoints
```

- [ ] El despliegue de Render termina en verde
- [ ] En los logs aparece `✓ Migraciones aplicadas correctamente`
- [ ] `SELECT COUNT(*) FROM "__EFMigrationsHistory"` devuelve **31**
- [ ] Los recuentos de `user_profiles` y `vehicles` coinciden con los de antes
- [ ] Ningún usuario se ha quedado sin `display_name`:
      `SELECT COUNT(*) FROM users.user_profiles WHERE display_name = ''` → **0**

### 0.3 Índices únicos parciales

⚠️ El proveedor en memoria de los tests **no los valida**: es la primera vez que existen
de verdad.

- [ ] `contracts.negotiation_id` — único `WHERE status <> 'Annule'`.
      Comprobación: anular un contrato y crear otro en la misma negociación **debe
      funcionar**; dos contratos vivos a la vez **no**
- [ ] `garage_vehicles.source_contract_id` — único `WHERE deleted_at IS NULL`.
      Un contrato no puede entrar dos veces en Mon Garage
- [ ] `feature_interests (feature_id, user_id)` — único `WHERE deleted_at IS NULL`.
      Retirar un «Ça m'intéresse» y volver a declararlo **debe funcionar**

### 0.4 Secuencias de referencias públicas

- [ ] `YU#####` en anuncios · `YD#####` en demandes · `YC#####` en contratos ·
      `SG#####` en signalements
- [ ] Publicar dos anuncios seguidos da referencias **distintas y consecutivas**
- [ ] Ninguna referencia sale vacía ni con formato raro

### 0.5 Datos sembrados (`HasData`)

- [ ] `platform_settings` tiene **una** fila (comparador 3, puntos 100, fotos 20)
- [ ] `feature_flags` tiene **cinco** filas, todas activas
- [ ] `upcoming_features` tiene **cinco** filas
- [ ] `price_indicator_settings` y `vehicle_valuation_settings`, una fila cada una

### 0.6 Entorno de Render

- [ ] `ConnectionStrings__DefaultConnection` apunta a Neon
- [ ] `Jwt__Key` tiene 32+ caracteres
- [ ] `Cors__AllowedOrigins` y `Frontend__Url` apuntan a la URL real de Vercel
- [ ] ⚠️ **QuestPDF necesita `libfontconfig1`** en Linux. Si falta, descargar el PDF de
      un contrato revienta con `DllNotFoundException`. Comprobarlo en el punto 3.7
- [ ] ⚠️ **Almacenamiento efímero**: el disco de Render se pierde en cada reinicio. Las
      fotos de anuncios y los documentos de Mon Garage **desaparecerán**. Es el pendiente
      técnico nº 2 y afecta a todo lo que se suba durante estas pruebas
- [ ] ⚠️ **Correo**: sin `Email__ApiKey` se usa `ConsoleEmailSender` y **no sale ningún
      correo**. Todo lo que dependa del correo hay que darlo por no probado

### 0.7 Frontend en Vercel

- [ ] El despliegue termina en verde
- [ ] La portada carga sin errores en la consola del navegador
- [ ] `GET /api/v1/platform/settings` responde 200 (sin autenticar)
- [ ] El idioma es **francés en todas partes** y los precios llevan **FCFA**

---

## 1. Cuenta e identidad — *doc §1*

- [ ] Registrarse con **teléfono** `+221XXXXXXXXX`, sin correo → funciona
- [ ] El mismo teléfono no admite dos cuentas
- [ ] Registrarse con correo opcional → también funciona
- [ ] Iniciar sesión con teléfono y contraseña
- [ ] Elegir **Particulier** o **Professionnel** en el perfil
- [ ] ⚠️ Ser `Professionnel` **no** da ningún privilegio: mismos límites, mismas
      acciones. Comprobar que publica igual que un `Particulier`
- [ ] Ciudad y región del perfil, de las 14 regiones de Senegal
- [ ] Refresco de token: dejar la sesión abierta más de 15 minutos y seguir navegando
- [ ] Cerrar sesión limpia el almacenamiento local

**Roles**

- [ ] Solo existen `User` y `Admin`. No hay rastro de Dealer, Seller, Buyer ni Moderator
- [ ] Un `User` que entra a `/admin` es rechazado
- [ ] Un visitante sin cuenta **no ve el teléfono** del vendedor

---

## 2. Etapa 1 — «Trouve ta voiture» — *doc §2*

### 2.1 Publicar un anuncio

- [ ] Publicar exige estar autenticado, y nada más
- [ ] Ficha completa: marca, modelo, versión, año, kilometraje, carburante, caja,
      carrocería, potencia, cilindrada, transmisión, puertas, plazas, color
- [ ] **Estado aduanero**: Dédouané · Non dédouané · Passavant
- [ ] Precio en **FCFA**, con el formato `8.900.000 FCFA`
- [ ] Equipamiento desde el catálogo (casillas, no texto libre)
- [ ] Región y ciudad de Senegal
- [ ] Fotos: se suben y se reordenan
- [ ] ❌ **No existe ningún botón de generar la descripción con IA** (lo prohíbe el doc)
- [ ] Al guardar como borrador, el anuncio queda en `Brouillon` y **no** es público
- [ ] Al publicar, pasa a `Actif` y recibe su `YU#####`

### 2.2 Buscar y filtrar

- [ ] Barra de búsqueda simple (marca, modelo, versión)
- [ ] Filtros: marca, modelo, precio, año, kilometraje, región, ciudad, estado aduanero,
      carburante, caja, carrocería, transmisión, potencia, equipamiento
- [ ] El **contador de resultados** cuadra con lo que se ve
- [ ] Ordenar: más reciente, precio ascendente, precio descendente, kilometraje
- [ ] Los borradores, pausados y archivados **no** aparecen
- [ ] Compartir el enlace de una búsqueda conserva los filtros

### 2.3 Ficha del anuncio

- [ ] Galería, ficha técnica, equipamiento, descripción, ubicación
- [ ] Referencia `YU#####` visible
- [ ] Vehículos similares al pie
- [ ] El contador de visitas sube al abrirla
- [ ] Estado del anuncio visible (Actif · Réservé · Vendu)
- [ ] Sin cuenta: el teléfono está oculto y se invita a registrarse

### 2.4 Indicador de precio — *doc §2.7*

- [ ] Con comparables suficientes aparece **Bonne affaire / Prix correct / Prix élevé**
- [ ] ⚠️ **Sin comparables suficientes no aparece nada** — ni un «sin datos», nada
- [ ] Los umbrales salen de `price_indicator_settings`, no del código: cambiarlos en
      `/admin/configuration` cambia el indicador
- [ ] ❌ El indicador **no** menciona inteligencia artificial en ninguna parte

### 2.5 Favoritos y alertas de precio

- [ ] Marcar y desmarcar favorito
- [ ] Bajar el precio de un favorito genera **notificación** al que lo tiene guardado
- [ ] El historial de precio del anuncio guarda **cada** cambio y no se puede borrar

### 2.6 Búsquedas guardadas

- [ ] Guardar una búsqueda desde el buscador, con nombre
- [ ] Publicar un anuncio que encaje genera notificación **una sola vez**
- [ ] ⚠️ La alerta salta al **publicar** (`Brouillon → Actif`), no al crear el borrador
      ni al reactivar tras una pausa
- [ ] Activar y desactivar la alerta de una búsqueda
- [ ] ⚠️ «Modifier» solo permite cambiar el **nombre**, no los criterios (pendiente nº 5)

### 2.7 Comparador — *doc §2.9*

- [ ] Añadir hasta **3** vehículos (el número sale de `platform_settings`)
- [ ] Al intentar el cuarto, avisa de que está lleno
- [ ] Cambiar el límite a 4 en `/admin/configuration` y comprobar que el comparador lo
      respeta **sin desplegar nada**
- [ ] ⚠️ La selección vive en `localStorage`: **no viaja a otro dispositivo**
      (desviación consciente de P10)

### 2.8 «Trouvez-moi cette voiture» — *doc §2.10*

- [ ] Crear una demande con marca, modelo, año, kilometraje, presupuesto y origen
- [ ] Recibe referencia `YD#####`
- [ ] Seguir su estado desde «Mes demandes»
- [ ] Cancelarla cuando aún se puede

### 2.9 Notificaciones (campana)

- [ ] La campana muestra el número de no leídas
- [ ] Marcar como leída, marcar todas
- [ ] Cada notificación lleva al sitio correcto
- [ ] Categorías: favoritos, búsquedas, negociación, contrato, admin, sistema

---

## 3. Etapa 2 — «Négocie et achète» — *doc §3*

> Todo cuelga de la **negociación**. Comprobar que no hay módulos sueltos de mensajes,
> ofertas ni contratos.

### 3.1 Abrir la negociación

- [ ] «Contacter le vendeur» sobre un anuncio `Actif` abre una negociación
- [ ] Dos veces sobre el mismo anuncio **no** abre dos negociaciones
- [ ] El vendedor no puede negociar consigo mismo
- [ ] Un anuncio `Vendu` o pausado no admite negociación nueva

### 3.2 Chat

- [ ] Enviar y recibir mensajes
- [ ] **Tiempo real**: con dos navegadores abiertos, el mensaje llega sin recargar
- [ ] Indicador de «escribiendo…» (`UserTyping`)
- [ ] Marcado de leído (`MessageRead`)
- [ ] Plantillas de respuesta rápida del vendedor
- [ ] ⚠️ **No se pueden enviar fotos en el chat** (pendiente nº 4; el doc sí las prevé)

### 3.3 Ofertas — *doc §3.4*

- [ ] Hacer una oferta con importe y mensaje
- [ ] El vendedor recibe notificación
- [ ] Contraoferta
- [ ] Aceptar y rechazar
- [ ] Una oferta aceptada cierra las demás pendientes
- [ ] El importe queda en la **cronología**, en orden

### 3.4 Cronología — *doc §3.6*

- [ ] Cada hito aparece: negociación abierta, mensajes, oferta, contraoferta, aceptación,
      contrato creado, contrato validado, venta verificada
- [ ] El orden es **estrictamente cronológico** y no se puede alterar
- [ ] ❌ Ningún hito se puede borrar

### 3.5 Checklist de inspección — *doc §3.5*

- [ ] Rellenar la checklist punto por punto
- [ ] ⚠️ **La otra parte NO la ve**: comprobarlo desde la otra cuenta
- [ ] Se puede seguir editando mientras la negociación esté abierta

### 3.6 Contrato — *doc §3.7*

- [ ] Crear el contrato desde la negociación
- [ ] Los datos del vehículo y de las partes quedan **congelados**: cambiar después el
      precio del anuncio **no** cambia el contrato
- [ ] Enviarlo a la otra parte
- [ ] La otra parte puede **pedir una modificación** con motivo
- [ ] Corregir y reenviar
- [ ] ⚠️ **Quien redacta no puede validar su propio contrato**: valida la otra parte
- [ ] Anular el contrato
- [ ] Tras anular, se puede crear otro en la misma negociación

### 3.7 Venta verificada y PDF — *doc §3.8*

- [ ] Validar el contrato produce, en una sola operación:
  - [ ] anuncio → `Vendu`
  - [ ] negociación → `Terminée`
  - [ ] vendedor → **+1 vente vérifiée**
  - [ ] vendedor → **+100 points** (o lo que diga la configuración)
  - [ ] el vehículo entra en el **Mon Garage del comprador**
- [ ] Descargar el **PDF** del contrato (⚠️ aquí falla si falta `libfontconfig1`)
- [ ] El PDF lleva el **código QR**
- [ ] Escanear el QR abre `/verification/:code` **sin necesidad de cuenta**
- [ ] ⚠️ Esa página pública **no** muestra CNI, direcciones ni teléfonos
- [ ] El código de verificación es aleatorio, **no** derivado de la referencia
- [ ] Un contrato en borrador **no** se puede descargar

---

## 4. Etapa 3 — «Mon Garage» — *doc §4*

### 4.1 Alta y ficha

- [ ] Añadir un vehículo a mano
- [ ] El comprado en la plataforma entra **solo** y **una sola vez**
- [ ] ⚠️ Mon Garage es **privado**: desde otra cuenta no se ve nada
- [ ] Editar la ficha
- [ ] ⚠️ El **kilometraje solo avanza**: intentar bajarlo debe ser rechazado

### 4.2 Documentos — *doc §4.3*

- [ ] Subir carte grise, assurance, visite technique, factura…
- [ ] ⚠️ **Descargarlos exige estar autenticado y ser el dueño**. Copiar la URL de
      descarga y abrirla desde una ventana privada **debe fallar**
- [ ] ⚠️ La API **nunca** devuelve la `StorageKey` del archivo: revisar la respuesta
      en la pestaña de red del navegador
- [ ] Borrar un documento borra el archivo de verdad, aunque la fila quede

### 4.3 Entretien — *doc §4.4*

- [ ] Registrar una intervención: fecha, kilometraje, tipo, taller, coste, notas
- [ ] Enlazar la factura a la intervención
- [ ] Adjuntar fotos
- [ ] Se agrupan por año
- [ ] El coste total del año cuadra

### 4.4 Recordatorios — *doc §4.5*

- [ ] Crear un recordatorio por **fecha**
- [ ] Crear un recordatorio por **kilometraje**
- [ ] Estados: À venir · À faire · Terminé · Annulé
- [ ] ⚠️ El recordatorio por kilómetros solo salta cuando el usuario **actualiza el
      kilometraje**: el sistema no lo inventa
- [ ] El trabajo en segundo plano corre cada 6 h y genera la notificación
      (para probarlo sin esperar, crear uno que venza hoy)

### 4.5 Valor estimado — *doc §4.6*

- [ ] Aparece una horquilla `8.200.000 – 8.600.000 FCFA`
- [ ] ⚠️ **Sin comparables suficientes no se muestra ninguna cifra**
- [ ] La evolución del valor se dibuja con las instantáneas
- [ ] ❌ En ningún sitio se dice que la estimación use inteligencia artificial

### 4.6 Complétude du dossier — *doc §4.7*

- [ ] La puntuación sube al añadir documentos, intervenciones y fotos
- [ ] ⚠️ El aviso de que **no es un diagnóstico mecánico** está visible en la pantalla,
      no escondido en un tooltip

### 4.7 «Vendre ce véhicule» — *doc §4.8*

- [ ] Crea un anuncio en **`Brouillon`**, nunca publicado directamente
- [ ] Hereda fotos y ficha técnica
- [ ] ⚠️ **No hereda precio, estado aduanero ni descripción**: son lo que el usuario
      debe revisar
- [ ] Transparencia: elegir qué parte del historial se hace pública
- [ ] ⚠️ Compartir una intervención **no** comparte su factura: son dos casillas
- [ ] Lo compartido se ve en el anuncio público desde una sesión cerrada
- [ ] Lo **no** compartido no se ve por ninguna vía

---

## 5. Mes annonces y navegación — *doc §5*

### 5.1 Mes annonces

- [ ] Estados: Brouillon · Actif · En pause · Réservé · Vendu · Archivé
- [ ] Cambiar de estado desde la lista
- [ ] Estadísticas por anuncio: visitas, favoritos, contactos
- [ ] **Score de calidad** del anuncio con lo que falta por completar
- [ ] Duplicar y archivar
- [ ] Sin límite de anuncios: publicar cinco seguidos con la misma cuenta

### 5.2 Navegación

- [ ] Menú público: **Voitures · Vendre · Trouvez-moi une voiture**
- [ ] Menú con cuenta: **Mes recherches · Mes négociations · Mon Garage · Mes annonces**
- [ ] Bajo el avatar: Mon profil · Paramètres · **Prochainement** · (Administration)
- [ ] La campana está en la cabecera, no en el menú
- [ ] Todo en francés, sin una sola palabra en español
- [ ] En móvil el menú se despliega y todos los enlaces funcionan

### 5.3 Prochainement — *doc §6.14*

- [ ] `/prochainement` muestra las cinco funcionalidades sembradas
- [ ] Sin cuenta se ve la lista pero **no** el botón «Ça m'intéresse»
- [ ] Con cuenta, «Ça m'intéresse» marca y desmarca
- [ ] Pulsar dos veces **no** cuenta por dos
- [ ] El contador de interesados se actualiza al instante

---

## 6. Backoffice — *doc §6*

### 6.1 Tableau de bord — *doc §6.2*

- [ ] Usuarios: total, altas hoy / 7 días / 30 días, particuliers vs professionnels
- [ ] Marketplace: activos, nuevos, reservados, vendidos, borradores, pausados
- [ ] **Anuncios pendientes de moderación**
- [ ] Actividad: negociaciones, mensajes, ofertas, contratos, ventas verificadas
- [ ] Demanda: búsquedas guardadas, favoritos, demandes pendientes
- [ ] Mon Garage: ⚠️ **solo números**, ningún contenido

### 6.2 Utilisateurs — *doc §6.3*

- [ ] Listado con búsqueda y filtros
- [ ] Ficha: perfil, actividad, acciones, notas internas, reportes recibidos
- [ ] Activar, **suspender** (con fecha) y **bloquear** una cuenta
- [ ] ⚠️ Toda medida **exige motivo** y deja fila en `admin_actions`
- [ ] Notas internas: crear y retirar (solo su autor)
- [ ] ⚠️ El usuario **nunca** ve las notas internas

### 6.3 Points de fidélité — *doc §6.9*

- [ ] La ficha muestra **saldo, origen, fecha y movimiento**
- [ ] La venta verificada aparece como `+100` con su referencia `YC#####`
- [ ] Ajuste manual: `+50 points · geste commercial`
- [ ] ⚠️ Un ajuste **sin motivo** es rechazado
- [ ] El ajuste deja fila en el journal con saldo anterior y nuevo
- [ ] El usuario recibe notificación del ajuste
- [ ] ⚠️ **Invalidar un contrato retira los puntos** con un movimiento en negativo, sin
      borrar el original

### 6.4 Annonces — *doc §6.4*

- [ ] Listado con filtros, incluido **reportado / no reportado**
- [ ] Ficha administrativa: métricas, historial de precios, acciones, notas
- [ ] Ocultar, reactivar, marcar para revisión, archivar, eliminar
- [ ] ⚠️ Ocultar usa `AdminHiddenAt`, **no** cambia el estado que eligió el vendedor
- [ ] ⚠️ El administrador **no puede editar** título, precio ni descripción: solo
      **pedir la corrección**
- [ ] La petición de corrección llega al vendedor como notificación

### 6.5 Demandes de véhicules — *doc §6.5*

- [ ] Cola de trabajo con estados
- [ ] Asignarse una demande y soltarla
- [ ] Proponer un vehículo **interno** (del marketplace)
- [ ] Proponer un vehículo **externo** (con fotos y enlace)
- [ ] Retirar una propuesta
- [ ] Responder al usuario
- [ ] Cambiar de estado con motivo

### 6.6 Négociations — *doc §6.6*

- [ ] El listado muestra la **estructura**: partes, vehículo, estado, número de mensajes
- [ ] ⚠️ **El contenido de los mensajes NO aparece en el listado ni en la ficha**
- [ ] Leer el contenido exige **elegir un motivo** y escribirlo
- [ ] ⚠️ Esa lectura queda registrada **en la misma operación**: comprobar que aparece
      en `admin_actions` con el nombre del administrador y el motivo

### 6.7 Contrats & ventes — *doc §6.7 y §6.8*

- [ ] Listado de contratos con su estado
- [ ] Consultar el PDF desde el backoffice
- [ ] Verificar el QR
- [ ] **Invalidar** un contrato con motivo
- [ ] Invalidar una venta verificada baja el contador del vendedor
      (y no lo deja en negativo si se repite)
- [ ] ⚠️ **El administrador NO puede validar** un contrato en nombre de las partes:
      comprobar que no existe ese botón por ninguna vía

### 6.8 Modération — *doc §6.10*

- [ ] Reportar un anuncio desde la ficha pública
- [ ] El signalement recibe referencia `SG#####`
- [ ] El anuncio aparece en el filtro «reportadas»
- [ ] Poner en examen (sin motivo obligatorio)
- [ ] **Cerrar** el signalement: exige explicar la decisión
- [ ] Al cerrarlo, el anuncio deja de aparecer como reportado
- [ ] Advertir al usuario señalado
- [ ] Pedir más información a quien reportó
- [ ] Un mismo usuario **no** puede abrir dos signalements abiertos sobre lo mismo

### 6.9 Communications — *doc §6.11*

- [ ] Enviar a **Tous**, a **Particuliers**, a **Professionnels** o a una persona
- [ ] Acotar por región
- [ ] Tipos: avis, maintenance, information importante, support
- [ ] ⚠️ Las cuentas **bloqueadas no reciben** el aviso
- [ ] El correo solo llega a quien tiene correo (⚠️ hoy no sale ninguno: sin API key)
- [ ] El histórico registra qué se envió, cuándo, por quién y a cuántos
- [ ] ⚠️ Un envío a «Tous» con muchas cuentas crea una notificación por persona en una
      sola operación. Con miles de usuarios **medir cuánto tarda** (pendiente nº 18)

### 6.10 Statistiques — *doc §6.13*

- [ ] Las cuatro ventanas: 7 j · 30 j · 90 j · 12 mois
- [ ] **Utilisateurs**: total, nuevos, activos, particuliers/professionnels, regiones
- [ ] **Offre**: activos, publicados, precio **mediano** (con la media al lado),
      kilometraje y año medianos, marcas, modelos, ciudades, carburante, aduana
- [ ] **Demande**: búsquedas guardadas, favoritos, demandes, presupuesto mediano,
      marcas más buscadas, modelos más en favoritos, filtros más usados
- [ ] ⚠️ **«Ce qu'on cherche et qu'on ne trouve pas»**: comprobar con un modelo real que
      las tres cifras (personas · demandes · anuncios) cuadran con la base de datos
- [ ] Una persona con tres búsquedas del mismo modelo cuenta **una vez**
- [ ] **Conversion**: vues → favoris → conversations → offres → accords → contrats →
      ventes vérifiées
- [ ] ⚠️ Las **vues son acumuladas**, el resto es del periodo (lo advierte la pantalla)
- [ ] ⚠️ Con la base ya poblada, **medir cuánto tarda**: las agregaciones se hacen en
      memoria (pendiente nº 19)
- [ ] Una búsqueda guardada con `filters_json` corrupto **no** deja el panel en blanco
- [ ] ❌ «Vehículos más comparados» **no se mide**, y la pantalla lo dice

### 6.11 Configuration — *doc §6.15*

- [ ] **Paramètres**: comparador, puntos por venta, fotos por anuncio, frescura,
      versión de las condiciones
- [ ] Umbrales del indicador de precio y de la estimación de valor
- [ ] ⚠️ Los valores fuera de rango son rechazados con mensaje en francés
      (probar comparador = 20, margen = 3)
- [ ] Guardar sin cambiar nada **no** ensucia el journal
- [ ] La fecha de las condiciones solo se mueve al **cambiar la versión**
- [ ] **Interruptores**: apagar uno y comprobar que la funcionalidad desaparece del front
- [ ] **Catálogos**: crear marca, crear modelo, crear equipamiento
- [ ] Dos marcas con el mismo nombre son rechazadas
- [ ] Dos modelos iguales de la **misma** marca, rechazados; de marcas distintas, no
- [ ] ⚠️ El **código** de un equipamiento **no cambia** al editarlo
- [ ] Retirar un equipamiento lo esconde del formulario **sin borrar** los anuncios
- [ ] El catálogo muestra cuántos anuncios usan cada entrada

### 6.12 Journal d'activité — *doc §6.16*

- [ ] Registra: administrador, acción, entidad, fecha/hora, **valor anterior y nuevo**,
      motivo
- [ ] Filtrar por administrador, por acción y por fechas
- [ ] ⚠️ Filtrar «hasta el día X» **incluye** ese día
- [ ] ❌ No hay forma de editar ni borrar una entrada
- [ ] Aparecen las acciones de todas las partes: cuentas, anuncios, demandes,
      conversaciones, contratos, signalements, puntos, parámetros y catálogos

### 6.13 Intérêt des utilisateurs — *doc §6.14*

- [ ] Ranking de funcionalidades por número de interesados
- [ ] Al elegir una: reparto **Particulier / Professionnel**
- [ ] Reparto por **ciudad**
- [ ] Reparto por **actividad** (sin anuncios · 1 · 2 a 5 · más de 5)
- [ ] Una funcionalidad retirada sigue apareciendo en el backoffice con su medición

---

## 7. Transversal

### 7.1 Idioma, moneda y geografía

- [ ] **Ni una palabra en español** en ninguna pantalla pública
- [ ] Todos los importes en **FCFA**, con el formato `8.900.000 FCFA`
- [ ] ❌ No aparece ningún €, $ ni ninguna otra divisa
- [ ] Las regiones y ciudades son las **14 de Senegal**
- [ ] Las fechas en formato francés `dd/MM/yyyy`

### 7.2 Privacidad

- [ ] Mon Garage: ni el administrador ve su contenido
- [ ] Checklist de inspección: solo su autor
- [ ] Conversaciones: solo las partes, y el administrador con motivo registrado
- [ ] Documentos privados: solo su dueño, por endpoint autenticado
- [ ] Página pública del QR: sin documentos de identidad ni datos de contacto

### 7.3 Identidad en los endpoints

- [ ] ⚠️ Ningún endpoint acepta `userId` por query string o por el cuerpo. Probar a
      añadir `?userId=<otro>` a una llamada y comprobar que **se ignora**

### 7.4 Tiempo real

- [ ] SignalR conecta tras iniciar sesión
- [ ] Chat: mensajes, «escribiendo», leído
- [ ] Notificaciones de oferta y de contrato llegan **en vivo** a la campana
      (esto se añadió en P16a y nunca se ha probado contra un servidor real)
- [ ] Reconexión automática tras perder la red

### 7.5 Rendimiento y errores

- [ ] Ninguna pantalla tarda más de 3 segundos con datos reales
- [ ] La consola del navegador no muestra errores
- [ ] Los logs de Render no muestran excepciones sin controlar
- [ ] Los errores de negocio devuelven mensaje en francés, no un 500

---

## 8. Recorrido completo (humo)

Un solo recorrido cubre casi toda la migración. Hacerlo con **dos cuentas** distintas.

1. [ ] Registrarse con teléfono `+221...` e iniciar sesión
2. [ ] Publicar un anuncio → tiene referencia `YU#####`
3. [ ] Buscar, filtrar y guardar la búsqueda; marcar favorito
4. [ ] Bajar el precio del favorito → llega la notificación
5. [ ] Desde la otra cuenta: contactar, ofertar → contraoferta → aceptar
6. [ ] Rellenar la checklist privada (la otra parte **no** debe verla)
7. [ ] Crear el contrato, enviarlo, pedir una modificación, corregir y validar
8. [ ] Comprobar: anuncio `Vendu`, negociación `Terminée`, +1 vente vérifiée, +100 points
9. [ ] Descargar el PDF y verificar el QR desde una ventana privada
10. [ ] El vehículo aparece **solo** en el Mon Garage del comprador
11. [ ] Subir un documento, registrar una intervención y enlazarle la factura
12. [ ] Crear un recordatorio y comprobar que salta
13. [ ] «Vendre ce véhicule» → sale un borrador con las fotos y **sin precio**
14. [ ] Compartir un entretien y su factura → verlos en el anuncio desde sesión cerrada
15. [ ] Desde `/admin`: consultar la negociación, leer el contenido con motivo, y
        comprobar que la lectura quedó registrada en el journal
16. [ ] Desde `/admin/statistiques`: comprobar que el recorrido aparece en el embudo

---

## 9. Módulos antiguos — pendientes de decisión

Todo lo de aquí es del producto anterior (*Logistique Les Lions*: import/export
multi-país con tramitación aduanera) y **no aparece** en `Yoon u Auto DOC APP.md`.
Sigue en el código, intacto. **Repásalo uno a uno y dime qué hacer con cada bloque:
eliminar, conservar o adaptar.**

### 9.1 Páginas que contradicen el documento

| # | Qué es | Ruta | Por qué contradice |
|---|---|---|---|
| 1 | **Planes de precio** | `/precios` | El doc dice que **todo es gratuito e ilimitado** para los usuarios. Esta página vende suscripciones de 49 € y 199 €/mes con límites por rol |
| 2 | **Concesionarios** | `/concesionarios` | El doc reduce «Professionnel» a un campo del perfil, **sin interfaz propia** ni listado de concesionarios |

> ⚠️ La sección de precios **ya se ha retirado de la portada** al reescribirla, pero la
> página `/precios` sigue existiendo y el pie ya no la enlaza.

### 9.2 Servicios del producto anterior

| # | Qué es | Ruta | Estado |
|---|---|---|---|
| 3 | **Tramitación aduanera** | `/tramitacion` + wizard, checklist documental, estimador de costes, guía de homologación, tracker | Muy extenso. Del doc solo sobrevive el *estado aduanero* como campo del anuncio |
| 4 | **Transporte internacional** | `/transporte` | No existe en el doc |
| 5 | **Financiación** | `/financiacion` | No existe en el doc |
| 6 | **Inspectores certificados** | `/inspectores` | El doc tiene checklist de inspección **privada del usuario**, no inspectores de plataforma |
| 7 | **Guías** | `/guias/importacion`, `/exportacion`, `/homologacion` | No existen en el doc |
| 8 | **Tracking público** | `/tracking` | Seguimiento de trámite por código. No existe en el doc |
| 9 | **Placeholders** | `/pagos`, `/valoraciones` | Sustituibles por *Prochainement*, que ya está construido |

Todos están **fuera del menú** desde P25: solo se llega por URL directa. Siguen en
español.

### 9.3 Componentes de la portada ya retirados

| # | Componente | Situación |
|---|---|---|
| 10 | `landing/country-map` | Mapa multi-país. **Ya no se usa** en la portada, el archivo sigue |
| 11 | `landing/newsletter` | **Ya no se usa** en la portada ni en el pie, el archivo sigue |
| 12 | `landing/stats-counters` | **Ya no se usa**. Mostraba cifras inventadas |

### 9.4 Backend sin uso en Yoon u Auto

| # | Qué es | Detalle |
|---|---|---|
| 13 | `ComplianceEndpoints` + `Features/Compliance` | Requisitos por país, costes, plantillas, procesos, documentos, incidencias |
| 14 | `CountryEndpoints` + `Features/Countries` | Catálogo multi-país. El doc es mono-país |
| 15 | `PublicTrackingEndpoints` + `Features/PublicTracking` | Seguimiento por referencia |
| 16 | `NewsletterEndpoints` | Suscripción por correo |
| 17 | `ExportEndpoints` | CSV de vehículos + PDF de albarán. ⚠️ El CSV **puede reutilizarse** en Statistiques |
| 17b | `GET /vehicles/facets` | Agregaciones tipo Amazon del producto anterior. **Devuelve 500 en producción** y el frontend no lo llama: se va con la decisión sobre el legacy |
| 18 | `Features/Marketplace` (partners) | ⚠️ **Colisión de nombres**: aquí «Marketplace» significa *partners de servicios*, no el marketplace de vehículos |
| 19 | **Generación de descripciones con IA** | 🔴 Ya **desactivada** en P3 (devuelve `Vehicle.AiDescriptionDisabled`). El código sigue. El doc **lo prohíbe expresamente** |
| 20 | **Extracción IA de documentos** | Sigue **activa** en el paso 1 del formulario. El doc no la prohíbe, pero tampoco la contempla |

### 9.5 Entidades de dominio

| # | Entidad | Observación |
|---|---|---|
| 21 | `Country`, `CountryRequirement`, `CustomsTariff`, `DocumentTemplate`, `HomologationRequirement` | Multi-país y aduanas |
| 22 | `ImportExportProcess`, `ProcessDocument`, `ProcessIncident` | Procesos de tramitación |
| 23 | `ServicePartner` | Transportistas, aduaneros, inspectores |
| 24 | `NewsletterSubscriber` | Suscriptores |
| 25 | `VehicleDocument`, `VehicleHistory` | ⚠️ **Ojo**: puede haber solape con Mon Garage. Revisar antes de tocar |

### 9.6 Backoffice heredado

| # | Sección | Observación |
|---|---|---|
| 26 | `/admin/procesos`, `/admin/incidencias`, `/admin/partners` | Siguen en el menú lateral, en español, ligadas a la tramitación |
| 27 | `dashboard-kpis.component.ts` | Huérfano: KPIs de procesos y lead time. Ya no lo usa nadie |

### 9.7 Otros

| # | Qué es | Observación |
|---|---|---|
| 28 | **Multi-divisa** | `Currency` en `Vehicle` y `Country`. El doc trabaja solo en FCFA |
| 29 | **Namespaces `LogistiqueLesLions.*`** | Renombrado a `YoonUAuto.*` aplazado a propósito |
| 30 | **Rutas en español** | `/mis-vehiculos`, `/mi-garaje`, `/mis-busquedas`, `/ajustes`, `/mis-pedidos`… conviven con una interfaz en francés. Traducirlas rompe los enlaces ya compartidos |

---

## 10. Deuda técnica conocida

No es parte de las pruebas, pero conviene tenerlo delante mientras se prueba. El detalle
está en [`docs/PENDIENTES-TECNICOS.md`](docs/PENDIENTES-TECNICOS.md).

| # | Bloqueante | Efecto durante las pruebas |
|---|---|---|
| 2 | **Almacenamiento efímero en Render** | Fotos y documentos **se pierden** en cada reinicio |
| 3 | **Correo sin configurar** | **No sale ningún correo** |
| — | **Frontend sin tests** | Todo el frontend se valida a mano |

---

## 11. Registro de la primera sesión de pruebas (2026-08-16)

Lo encontrado probando contra producción con Playwright, por orden de gravedad. Todo
está corregido y desplegado salvo donde se indique.

| # | Fallo | Qué rompía |
|---|---|---|
| 1 | **Cultura `de-DE` en imagen alpine** | La imagen corre en modo *globalization-invariant*, donde pedir cualquier cultura lanza. Tumbaba **las ofertas, las alertas de bajada de precio, los recordatorios de Mon Garage y el PDF del contrato**, todos con 500. Invisible en local |
| 0 | 🔴 **Bypass de autenticación** | `POST /auth/refresh` con `{"refreshToken": null}` devolvía **un token de administrador sin credencial alguna**. La consulta casaba con la primera cuenta de token nulo y el guardián de caducidad no frenaba porque `null < ahora` es falso. **El logout pone el token a nulo**, así que la exposición alcanzaba a toda cuenta que hubiera cerrado sesión alguna vez, no solo a la sembrada. Corregido y con siete pruebas |
| 2 | **Refresco de token en carrera** | El servidor rota el refresh token y el interceptor lanzaba uno por petición fallida: dos a la vez ⇒ el segundo usaba un token consumido ⇒ **cierre de sesión cada 15 minutos** |
| 3 | **Binding de `[AsParameters]`** | Un `int` o `bool` no anulable es obligatorio en las minimal APIs: `/vehicles/count` y `/notifications` devolvían 400 con cuerpo vacío. **Campana muerta y contador de resultados roto** |
| 4 | **Sin selector de modelo al publicar** | Todo anuncio salía con `modelId` nulo ⇒ **nunca podía tener indicador de precio** ni aparecer en filtros por modelo |
| 5 | **Sin selector de equipamiento** | Los anuncios se publicaban con `equipmentIds` vacío ⇒ el filtro por equipamiento no podía dar resultados |
| 6 | **Faltaban Pick-up y Monospace** | No se podía publicar un Hilux, el vehículo más vendido en Senegal |
| 7 | **`Lpg` inexistente en `FuelType`** | 400 al publicar un vehículo de gas |
| 8 | **`fontconfig` ausente en la imagen** | El PDF del contrato habría fallado igualmente tras corregir el nº 1 |
| 9 | **Interfaz en español** | Banner de cookies, formulario de publicación completo, títulos de pestaña, «Vehículos destacados», enum crudos en Statistiques |
| 10 | **`BodyType.Pickup` mal escrito** | El filtro de pick-up de la portada no devolvía nada |
| 11 | **Datos europeos heredados** | 20 anuncios con precios en euros etiquetados FCFA. Sustituidos por 48 senegaleses |

### Verificado funcionando

- Las 31 migraciones aplicadas y PostgreSQL sano
- Registro por teléfono, con correo opcional y las 14 regiones
- Publicación completa de un anuncio, con referencia `YU10025`
- **Indicador de precio en sus dos mitades**: con ≥5 comparables muestra la etiqueta y
  de cuántos sale; por debajo **no muestra nada**
- Búsqueda, filtros, contador, orden por precio, encadenado región → ciudad
- Ficha del anuncio con estado aduanero y su aviso de no verificación
- Backoffice: tableau de bord y **Statistiques con mediana (7.600.000) frente a media
  (8.761.224)**, y «—» donde no hay datos

---

## 12. Qué queda por probar a mano

Lo verificado automáticamente contra producción está en el apartado 11. **Esto es lo que
falta**, agrupado por lo que necesita: casi todo lo pendiente exige varias cuentas
distintas, esperar a que corra un proceso, o mirar dos pestañas a la vez — cosas que se
hacen mejor a mano.

> Se va actualizando conforme avanzan las pruebas. Lo que aparece aquí **no está
> probado**: no significa que falle, significa que nadie lo ha mirado todavía.

### 12.1 Necesita dos personas o dos dispositivos

- [ ] **Chat en tiempo real**: dos navegadores abiertos, el mensaje llega sin recargar
- [ ] Indicador «escribiendo…» y marcado de leído
- [ ] **Notificaciones en vivo** por SignalR al recibir una oferta o validarse un contrato
- [ ] ⚠️ **La conexión de SignalR no se rehace al refrescar el token** (pendiente nº 22):
      comprobar cuánto tarda en notarse y si molesta en uso real
- [ ] ⚠️ **Una sola sesión por cuenta** (pendiente nº 25): entrar desde el móvil expulsa
      la sesión del ordenador. Decidir si se acepta

### 12.2 Necesita esperar

- [x] **Rappels de Mon Garage**: no hizo falta esperar. Un recordatorio por kilometraje
      salta **en el acto** al actualizar el contador, con su notificación `reminder`
      (§12.5). Queda sin probar solo el disparo autónomo del trabajo de las 6 h
- [x] **Alerta de bajada de precio**: bajado `YU00042` de 4.700.000 a 4.400.000 y llegó
      la notificación «Baisse de prix» (categoría `price-drop`) al instante
- [ ] **Alerta de búsqueda guardada**: publicado `YU10026` con una búsqueda guardada sin
      filtros y alerta activa, y **no llegó nada** — correctamente: el servicio excluye a
      quien publica (`NewVehicleAlertService.cs:27`, «Quien publica no necesita que le
      avisen de su propio anuncio»). **Sigue sin poder probarse de punta a punta** con una
      sola cuenta, igual que lo de «una sola vez» y lo de no repetir tras una pausa
- [ ] **Caducidad del token**: dejar la sesión abierta más de 15 minutos y seguir
      navegando sin que se cierre (corregido, pero conviene confirmarlo en uso real)

### 12.3 Etapa 1

- [x] Favoritos: marcar, desmarcar, y que el contador del anuncio cambie *(9 → 10 → 9)*
- [x] Búsquedas guardadas: guardar desde el buscador, activar y desactivar su alerta
- [x] Comparador: que **cambiar el límite en Configuration lo cambie sin desplegar**
      *(la ficha pasó a «Comparer (0/4)» sin redesplegar nada)*
- [ ] ❌ **Al llenarse, el comparador no avisa: expulsa uno en silencio.** Con cuatro
      seleccionados, el botón sigue activo, marca «(3/4)» —una menos de las que hay— y al
      pulsarlo entra el quinto y desaparece uno de los anteriores. El doc pide avisar de
      que está lleno
- [x] «Trouvez-moi cette voiture»: crear una demande, ver su `YD00001`, cancelarla
- [x] Que el **teléfono del vendedor esté oculto** para quien no tiene cuenta
      *(el DTO del anuncio no trae ningún teléfono, ni siquiera autenticado)*

### 12.4 Etapa 2

Probada el 16/08/2026 con dos cuentas reales, `+221771234501` (vendedor) y
`+221771234500` (comprador), en cuanto se recuperó un administrador con contraseña
conocida.

- [x] **Rechazar una oferta**: pasa a `Refusee` y la negociación sigue abierta
- [x] Contraoferta del vendedor y aceptación del comprador: la aceptada queda `Acceptee`
      y la anterior conserva su `Refusee` en el histórico
- [x] **Crear el contrato** y enviarlo a la otra parte
- [x] ⚠️ **Quien redacta no puede validar lo suyo**: el vendedor recibe
      `Contract.NotValidator` (400) al intentarlo
- [x] **Pedir una modificación**, corregir y reenviar: los tres pasos, con sus estados
- [x] 🔑 **Anular un contrato y crear otro en la misma negociación**: funciona. Es la
      primera vez que se valida el **índice único parcial `contracts.negotiation_id`**
      contra PostgreSQL — el proveedor en memoria de los tests no lo comprueba
- [x] **Validación por la otra parte** y venta verificada completa:
      anuncio `YU10028` → **`Vendu`**, negociación → **`Terminee`**, contrato `YC00004`
      → **`Valide`**, vendedor → **1 vente vérifiée** con sus puntos
- [ ] Plantillas de respuesta rápida del vendedor — sin probar

**Lo que no cuadra con el plan:**

- [ ] ⚠️ **El vehículo comprado NO entra solo en Mon Garage.** El apartado 3.7 de este
      documento lo daba por hecho, pero `ValidateContractCommandHandler` solo mueve el
      anuncio a `Vendu`, cierra la negociación, suma la venta verificada y los puntos.
      Lo que existe es `GET /garage/from-contract/{id}`, que devuelve la ficha
      **precargada** con `alreadyAdded: false` para que **el comprador decida** añadirlo,
      con `SourceContractId` como guardia para que no entre dos veces.
      Comprobado: tras validar, el garaje del comprador seguía con **0 vehículos** y el
      prefill respondía 200. Es una decisión de producto, no un fallo: hay que elegir si
      se añade solo o se sigue ofreciendo.

### 12.5 Mon Garage

Recorrido entero el 16/08/2026 sobre un vehículo creado a mano (Toyota Land Cruiser 2018).

- [x] ⚠️ **Mon Garage es privado**: el garaje del comprador tiene 1 vehículo y desde la
      cuenta del administrador se ven **0**
- [x] Alta a mano con marca → modelo encadenado, y ficha completa
- [x] **Rappels**: creado uno por fecha (vence hoy → «À faire») y otro por kilometraje
      (100.000 km → «À venir · 1.000 km restants»)
- [x] ⚠️ **El de kilómetros solo salta al actualizar el contador**: al pasar de 99.000 a
      100.500 km cambió a «À faire · dépassé de 500 km» **y llegó su notificación**
      (`reminder`). No hizo falta esperar las 6 h del trabajo en segundo plano
- [x] Estados del recordatorio: «À faire» → «Terminé», con opción de «Rouvrir»
- [x] Que el **kilometraje no pueda bajar**: la API responde
      `GarageVehicle.MileageWentBackwards` (400) y el valor no se mueve
- [x] **Entretien**: intervención con fecha, km, tipo, taller, coste y notas; **agrupada
      por año** y con el total correcto («1 intervention · 85.000 FCFA au total»)
- [x] **Factura enlazada** a la intervención («Facture disponible ✓»)
- [x] **Documentos**: subido un PDF; la API **no devuelve la `StorageKey`** (solo el
      nombre original del fichero); la descarga da 200 con token y **401 sin él**, y el
      fichero **no** se sirve desde `/uploads` (404). La propiedad se comprueba en el
      handler (`document.GarageVehicle.UserId != userId` → 403)
- [x] **Valeur estimée**: «Pas assez de données pour estimer la valeur» — ninguna cifra
      inventada, y **sin mencionar inteligencia artificial**
- [x] ⚠️ **Complétude**: el aviso de que **no es un diagnóstico mecánico** está en la
      propia pantalla, no en un tooltip
- [x] **«Vendre ce véhicule»**: crea `#YU10026` en **`Brouillon`**, no visible, y
      **precio `0`, estado aduanero `null` y descripción `null`**. Sí hereda marca,
      modelo, año, km, carburante, caja, carrocería, potencia y color
- [x] ⚠️ **Transparence — «son dos casillas», confirmado**: compartida la intervención
      **sin** su factura, la respuesta pública trae el registro con
      `invoiceDocumentId: null`, y pedir la factura por su id real da **404
      `Transparency.NotShared`**. Al marcar también la factura, se descarga (200, PDF).
      Al desmarcar todo, la transparencia vuelve a venir vacía
- [x] ⚠️ Sin marcar «dates et kilométrage», la respuesta pública devuelve `performedAt` y
      `mileage` a **null**: no se filtra solo en pantalla
- [x] Ver lo compartido **desde sesión cerrada** *(comprobado sin token contra la API)*
- [ ] Fotos: se suben y se marcan «Principale», pero ver el fallo de abajo
      (⚠️ además se perderán al reiniciar Render, pendiente nº 2)
- [ ] Que «Vendre ce véhicule» **herede las fotos**: el borrador se creó cuando el garaje
      aún no tenía ninguna, así que dice «0 photo(s) reprise(s)». Sin comprobar
- [ ] Que el vehículo comprado en la plataforma entre **una sola vez**: hace falta la
      cuenta del comprador

**Lo que falló:**

- [ ] 🔴 **Las fotos de Mon Garage se sirven sin autenticación.** Van a `/uploads/`, que
      es la carpeta estática: `GET .../uploads/garage/<id>/<guid>.png` devuelve **200
      image/png sin token**. Los documentos sí están bien (401), pero las fotos no. El
      identificador es un GUID, o sea que la protección es solo que nadie adivine la URL.
      Contradice la regla de Mon Garage privado
- [ ] ❌ **La API devuelve las URLs de fichero en `http://`**, no en `https://`. Chrome
      registra «Mixed Content» y las eleva él solo, pero con una política más estricta la
      imagen no cargaría
- [ ] ⚠️ **El panel de complétude no se refresca**: al cerrar un recordatorio siguió
      diciendo «2 rappels en retard» hasta recargar la página
- [ ] ⚠️ **La complétude puede bajar al añadir cosas**: pasó de 50 % a 45 % tras añadir un
      documento y una intervención, porque los dos recordatorios recién creados estaban
      vencidos y eso pesa más. No es un fallo, pero el doc dice que «la puntuación sube al
      añadir documentos e intervenciones» y no siempre es así
- [ ] ⚠️ Subir una factura de entretien **no** quita el aviso «Aucun document essentiel»:
      cuentan carte grise, assurance y contrôle technique. Coherente, pero conviene saberlo
- [ ] ⚠️ El rechazo del kilometraje hacia atrás **tampoco se explica en pantalla**: mismo
      patrón mudo que en el backoffice

### 12.6b Observaciones de Mes annonces

- [ ] ⚠️ **Se puede cambiar el precio de un anuncio ya vendido**. La acción «Prix» sigue
      ofreciéndose sobre un anuncio en estado `Vendu`. No corrompe nada —el contrato
      congela el precio acordado y las estadísticas solo miran los activos— pero cambia
      lo que ve quien abre un anuncio vendido. Decidir si se retira la acción
- [ ] ⚠️ La cuenta de pruebas `+221770000101` **posee 3 anuncios del catálogo sembrado**,
      porque existía antes de que corriera el reseed. Tenerlo en cuenta al limpiar
- [ ] ⚠️ **Confirmado**: la acción «Prix» sigue ofreciéndose sobre `YU10025`, que está
      `Vendu`. Y también «Kilométrage»

### 12.6 Mes annonces

- [x] Los **seis estados** con su recuento, y las acciones cambiando según el estado:
      un `Brouillon` ofrece «Publier l'annonce», un `Actif` ofrece «Mettre en pause /
      Marquer réservé / Marquer vendu», un `Réservé` ofrece «Remettre en vente»
- [x] Cambios de estado **desde la lista**: `Actif → Réservé → Actif`, con los contadores
      de las pestañas actualizándose al momento
- [x] Estadísticas por anuncio: visitas, favoritos y contactos, distintas en cada uno
- [x] **Score de calidad**: reacciona de verdad. El borrador del Land Cruiser marcaba
      15 %; al ponerle precio subió a **30 %**
- [x] **Duplicar**: crea `YU10027` en `Brouillon` con referencia **consecutiva**. Sí
      hereda el precio, a diferencia de «Vendre ce véhicule» — y es lo correcto:
      duplicar es repetir un anuncio, no estrenar uno
- [x] **Archivar**: sale de la lista por defecto y **desaparece del escaparate**
      (contador público 47 → 46)
- [x] **Publicar**: `Brouillon → Actif`, con `publishedAt` y el contador público
      46 → 47
- [x] ✅ **Un borrador sin precio no se puede publicar**, y aquí el rechazo **sí se
      explica**: «Publication impossible. Vérifiez que l'annonce a un prix.» Es la
      excepción a los rechazos mudos del resto de la aplicación
- [x] Sin límite de anuncios: la cuenta es `Particulier` y llegó a **6 anuncios, 3 de
      ellos activos a la vez**, sin que apareciera ninguna restricción

**Observado, sin ser fallo:**

- Un anuncio `Réservé` **sigue apareciendo en el buscador**, con su etiqueta; los
  `Vendu`, `En pause`, `Brouillon`, `Archivé` y los ocultados, no. Encaja con el doc
  (§2.2 solo excluye borradores, pausados y archivados; §2.3 pide que se vea el estado)

### 12.6c Invalidar un contrato: qué pasa con el anuncio

Verificado que invalidar **sí** revierte la reputación: el saldo de puntos vuelve a 0 con
un movimiento `-100 VenteInvalidee` que no borra el `+100` original, el contador de
ventas verificadas baja, y la página pública del QR deja de verificar (404).

Pero quedan dos cosas **como estaban** y conviene decidir si es lo que se quiere:

- [ ] El **anuncio sigue en `Vendu`**. El vendedor puede recuperarlo, pero por un camino
      poco evidente: Archiver → Remettre en brouillon → volver a publicar. ¿Debería
      invalidar devolverlo a `Actif`, u ofrecer «Remettre en vente» desde `Vendu`?
- [ ] La **negociación sigue en `Terminée`**. Si el contrato se invalidó por fraude,
      quizá deba reabrirse; si fue un error administrativo, quizá no

### 12.7 Backoffice

Recorrido entero el 16/08/2026. Lo verificado:

- [x] **Utilisateurs**: suspender con fecha, bloquear, reactivar, notas internas, y que
      **toda medida exija motivo** (`Admin.ReasonRequired`, 400)
- [x] **Annonces**: ocultar deja el anuncio en `Actif` + «masquée» sin tocar el estado del
      vendedor; **no hay ningún campo para editar** título, precio ni descripción, solo
      «Demander une correction», que queda registrada
- [x] **Demandes**: asignarse, proponer vehículo interno y externo, retirar la propuesta,
      responder y cambiar de estado con motivo
- [x] ⚠️ **Négociations**: el contenido no aparece ni en el listado ni en la ficha;
      leerlo exige elegir motivo **y** escribir por qué; la lectura queda registrada en
      «Accès enregistrés» y en el journal, con nombre y motivo
- [x] **Contrats**: **no existe ningún botón de validar** por ninguna vía; la pantalla lo
      dice explícitamente
- [x] **Modération**: reportar desde la ficha pública → `SG00001`; el mismo usuario no
      puede abrir un segundo signalement («déjà signalé»); «En examen» no pide motivo;
      cerrar **sí** lo exige y queda con su decisión
- [x] **Communications**: envío a «Tous» → **9 destinatarios de 10 usuarios**, la cuenta
      bloqueada quedó excluida; el histórico registra qué, cuándo, quién y a cuántos.
      ⚠️ El correo sigue sin probar: no hay API key (pendiente nº 3)
- [x] **Configuration**: comparador = 20 → «Le comparateur doit accepter entre 2 et 6
      véhicules.»; margen = 3 → «La fourchette doit être comprise entre 0 et 1.»;
      guardar sin cambios **no** añade fila al journal; marca y modelo duplicados
      rechazados, el mismo modelo en otra marca admitido; el **código de un equipamiento
      no cambia** al renombrarlo (probado contra producción)
- [x] **Journal**: administrador, acción, motivo, fecha y **valor anterior → nuevo**;
      filtrar «Du = Au = hoy» devuelve las entradas de hoy, o sea que **incluye** ese día
- [x] **Points**: ajuste sin motivo rechazado; `+50` y `−50` con motivo dejan los dos
      movimientos y el saldo vuelve a 0 sin borrar nada

**Lo que falló:**

- [ ] ❌ **El listado de negociaciones cuenta 12 y enseña 1.** La API devuelve
      `totalCount: 12` con un solo elemento: el recuento se hace antes de proyectar, y la
      proyección lee `n.Vehicle`, cuyo filtro de borrado lógico convierte la consulta en
      un *inner join*. Afecta también a la paginación. **Corregido, pendiente de
      desplegar**
- [ ] ❌ **Falta el filtro «reportadas» en Annonces.** La API acepta `Reported`, pero el
      formulario solo expone «Masquées» y «À réviser». El doc §6.4 lo pide
- [ ] ❌ **No se puede consultar el PDF del contrato ni verificar el QR desde el
      backoffice.** La ficha enseña el código de verificación como texto, pero no hay
      enlace ni descarga. El doc §6.7 pide ambas cosas
- [ ] ❌ **Enums crudos en francés a medias**: el journal escribe `Dispute` en vez de
      «Litige entre les parties», y el historial de un signalement escribe `EnExamen`.
      Es el mismo fallo que se corrigió en la ficha de negociación (commit 239cca6)
- [ ] ❌ **El historial de un signalement rotula «Signalement clôturé» también al ponerlo
      en examen**, que no lo cierra
- [ ] ❌ **Al proponer un vehículo interno, el buscador ofrece anuncios vendidos**
      (`YU10025`, en estado `Vendu`, aparece entre las propuestas posibles)
- [ ] ⚠️ **Los rechazos no se explican.** Suspender sin motivo, leer una conversación sin
      justificarla o ajustar puntos sin motivo **no hacen nada y no dicen por qué**: el
      botón sigue activo y no aparece mensaje. La regla se cumple, pero el administrador
      no sabe qué le falta
- [ ] ⚠️ Tras un error de catálogo, el aviso («Cette marque existe déjà») **se queda en
      pantalla** aunque la acción siguiente funcione
- [ ] ⚠️ Falta de ortografía: «**Anexer** une annonce Yoon u Auto» → *Annexer*

### 12.8 Transversal

- [x] ⚠️ Que **ningún endpoint acepte `userId` por query string**: añadido
      `?userId=<otro>` a `/notifications`, `/saved-searches` y `/negotiations`, y la
      respuesta es **byte a byte idéntica** a la de la llamada sin él
#### Repaso de idioma — hecho el 16/08/2026

Barrido automático de todas las pantallas buscando palabras inequívocamente españolas,
símbolos de divisa y formatos de fecha.

- [x] **Yoon u Auto está limpio.** Ni una palabra en español en: portada, Marketplace,
      ficha de anuncio, `/prochainement`, `/comparateur`, `/dashboard`, `/favoritos`,
      `/mis-negociaciones`, `/mensajes`, `/perfil`, `/ajustes`, `/mis-vehiculos`,
      `/mi-garaje`, el formulario de publicar, el de añadir al garaje y **todo el
      backoffice nuevo**. Ninguna divisa que no sea FCFA
- [ ] 🔴 **Las cinco páginas legales tienen el título en francés y el cuerpo en español —
      y describen una empresa española.** No es solo idioma:
  - `/legal/aviso-legal` → «Yoon U Auto, **S.L.** … Domicilio social: Calle de ejemplo,
    123, 28001 **Madrid, España**. NIF: B-12345678. **Registro Mercantil de Madrid**»,
    invocando la **Ley 34/2002 (LSSI-CE)** española
  - `/legal/rgpd` → Reglamento (UE) 2016/679 y **Ley Orgánica 3/2018**, normativa europea
    y española, no senegalesa
  - `/legal/cookies` → «¿Qué son las cookies?»; `/legal/terminos` → «1. Descripción del
    servicio», «2. Registro y cuenta de usuario»
  - Las cinco llevan «**Volver al inicio**» y «Última actualización: Enero 2025»
  - ⚠️ **Están enlazadas desde el pie de todas las páginas**, así que cualquiera llega
- [ ] ⚠️ **`/admin/procesos`, `/admin/incidencias` y `/admin/partners` siguen en el menú
      lateral del backoffice**, en español y con datos europeos («Gestoría Iberia»,
      «Carfax Europe Inspectors»). Incidencias enseña además enums crudos: «Medium»,
      «Open», «Resolved». Sus títulos de pestaña rompen el patrón: «Procesos — Admin» en
      vez de «— Administration»
- [ ] ⚠️ **Quedan euros en producción**: `/precios`, `/transporte` y `/financiacion`
      muestran importes en **€**. Solo se llega por URL directa, pero están vivas.
      Es el ❌ de §7.1 («no aparece ningún €»), pendiente de la decisión sobre el legacy

#### Rendimiento — medido el 16/08/2026

- [x] **Ninguna pantalla se acerca a los 3 segundos.** Navegación entre rutas: 51–215 ms.
      Carga completa del Marketplace: **DOM 144 ms, primer pintado 228 ms**, 24 ficheros
      JS. Las llamadas más lentas de esa pantalla: listado 304 ms, notificaciones 219 ms,
      marcas 191 ms
- [x] **API**: todo por debajo de 350 ms. Backoffice 110–346 ms; catálogos 150 ms (12 kB)
- [x] **Statistiques**: 254 ms (7 j), 275 ms (30 j), 302 ms (90 j), **308 ms (12 mois)**
- [ ] ⚠️ Pero eso **no responde al pendiente nº 19**: con 10 usuarios y 49 anuncios, unas
      agregaciones en memoria van rápidas por fuerza. Sigue sin probarse con volumen
- [ ] ⚠️ **Todo se midió con la API caliente.** No se ha medido el arranque en frío de
      Render —el caso que se lleva el primer visitante del día— ni la latencia real desde
      Senegal: el navegador de pruebas no está allí
- [x] La consola no muestra errores propios de la aplicación. Los únicos son los del
      punto siguiente

#### SignalR no se recupera del refresco de token — confirmado en vivo

- [ ] 🔴 Al caducar el token, el `negotiate` del hub recibe **401 y el cliente no lo
      reintenta nunca**. Comprobado que el token nuevo **sí vale**: repitiendo el
      `negotiate` a mano con él, responde **200**. O sea que no es un problema de
      permisos, es que falta el reintento. Efecto práctico: **pasados los 15 minutos, las
      notificaciones en vivo dejan de llegar en silencio** y solo reaparecen al recargar.
      Es el pendiente nº 22, ahora con su impacto medido

### 12.9b Últimas pruebas en solitario (2026-08-16)

Lo que quedaba y no exigía una segunda cuenta.

**Prochainement — *doc §6.14***

- [x] `/prochainement` muestra las **cinco** funcionalidades sembradas
- [x] «Ça m'intéresse» marca y desmarca, y el contador cambia **al instante**
      (0 → 1 → 0 → 1), con el singular y el plural bien puestos
- [x] ⚠️ **Índice único parcial `feature_interests` validado**: retirar el interés y
      volver a declararlo funciona, y **dos peticiones simultáneas no lo cuentan por
      dos** (el contador nunca pasa de 1). Era uno de los tres índices que los tests en
      memoria no comprueban

**Notificaciones (campana) — *doc §2.9***

- [x] La campana muestra el número de no leídas (13) y desaparece al quedarse en cero
- [x] Marcar una como leída baja el contador (13 → 12)
- [x] **Cada notificación lleva al sitio correcto**: «Baisse de prix» abrió justo
      `/vehiculos/peugeot-208-2020`, el anuncio cuyo precio había bajado
- [x] «Tout marquer comme lu» deja el contador a 0, confirmado también por la API
- [x] Categorías vistas en producción: `price-drop`, `reminder`, `offer`, `contract`,
      `message`, `request-proposal`, `admin`, `system`

**Statistiques — *doc §6.13***

- [x] Las cuatro ventanas, y **precio mediano (7.600.000) con la media al lado
      (8.692.553)**
- [x] Kilometraje y año medianos, marcas, modelos, ciudades, carburante y aduana
- [x] «Budget médian recherché» muestra **«—»** cuando no hay datos: no se inventa nada
- [x] ❌ La pantalla **dice** que los vehículos más comparados no se miden, y por qué
- [x] El aviso de que las vistas son acumuladas y el resto del periodo está visible
- [ ] ⚠️ **La columna «Personnes» de «Ce qu'on cherche et qu'on ne trouve pas» se lee
      mal**: para Toyota Hilux marca «Personnes 0 · Demandes 1», que parece imposible.
      No lo es: «Personnes» cuenta *quien tiene una búsqueda guardada apuntando a ese
      modelo*, y la demande vino por otra vía. El dato es correcto; la etiqueta engaña
- [ ] ⚠️ **Una demande cancelada sigue contando** en esa tabla (`YD00001` está `Annulée`)
- [ ] ⚠️ **El embudo enseña porcentajes por encima del 100 %**: «Offres 200 %»,
      «Contrats 100 %». La API devuelve recuentos crudos (vues 21279, favoris 2,
      négociations 1, offres 2, contrats 1) y el porcentaje lo calcula el frontend
      **sobre el paso anterior**, así que 2 ofertas en 1 negociación dan 200 %. Es
      legítimo como ratio, pero en algo llamado «embudo» se lee como un error
- [ ] Un `filters_json` corrupto no deja el panel en blanco: **no se ha podido probar**,
      haría falta escribir directamente en la base de datos

**Navegación en móvil — *doc §5.2***

- [x] A 390 px el menú se pliega tras «Ouvrir le menu» y se despliega con **todos** los
      enlaces: Mes recherches · Mes négociations · Mon Garage · Mes annonces · Mon profil ·
      Paramètres · Prochainement · Administration, más los accesos por carrocería
- [x] **Ningún enlace sin destino** y **sin desbordamiento horizontal**
- [x] La campana está en la cabecera, no dentro del menú

### 12.10 Registro de la segunda sesión (2026-08-16, tarde)

Recorrido del backoffice entero, la Etapa 1 y lo transversal. El detalle por bloques está
arriba; esto es lo que hay que decidir o desplegar.

#### Lo más grave: ocultar un anuncio no lo oculta

🔴 **Un anuncio sin página pública se sirve entero a cualquiera que tenga el enlace.**
`GET /api/v1/vehicles/{slug}` no miraba quién preguntaba: devolvía 200 con todos los datos
para un anuncio en `Brouillon`, `EnPause`, `Archive` **y para uno ocultado por
moderación**. Comprobado en producción sin token con `renault-duster-2021` (en pausa) y
con `peugeot-208-2019-2` (ocultado a las 11:48 desde el backoffice): los dos responden
200. Ocultar solo lo quitaba del buscador.

Corregido: la consulta recibe ahora la identidad del token y devuelve `Vehicle.NotFound`
—no «prohibido», que ya delataría que existe— salvo que pregunte su dueño o un
administrador. Un anuncio **vendido** conserva su página, porque de ella cuelgan el
contrato y los favoritos. **Pendiente de desplegar.**

#### Segundo agujero de privacidad: las fotos de Mon Garage

🔴 **Las fotos de un vehículo de Mon Garage se sirven a cualquiera, sin autenticación.**
Se guardan en `/uploads/`, la carpeta estática, y
`GET https://…/uploads/garage/<vehículo>/<guid>.png` responde **200 image/png sin token**.
Los **documentos** sí están bien resueltos —401 sin token, y ni siquiera aparece la
`StorageKey` en el DTO—, pero las fotos se quedaron por el camino público. Lo único que
protege una foto hoy es que su URL lleve un GUID.

En el código está a la vista: `GarageEndpoints.cs:134` sube las fotos con
`storage.UploadAsync(...)`, mientras que las líneas 205 y 380 —documentos y facturas— usan
`UploadPrivateAsync`.

**No lo he corregido porque no es un descuido evidente, sino una tensión de diseño.** Las
fotos del garaje están en la carpeta pública *precisamente* porque «Vendre ce véhicule»
las hereda en el anuncio, y las fotos de un anuncio tienen que ser públicas. Pasarlas a
`UploadPrivateAsync` obliga a copiarlas o a republicarlas al crear el borrador. Las
opciones son:

1. Servirlas por endpoint autenticado y **copiarlas a la carpeta pública** al pulsar
   «Vendre ce véhicule» — es lo que respeta la regla, y cuesta un paso más.
2. Dejarlas donde están y **asumir** que la foto de un coche del garaje es adivinable solo
   con su GUID.

Hace falta tu decisión antes de tocar nada.

De paso: la API devuelve esas URLs en **`http://`**, no `https://`. Chrome las eleva solo
y deja un aviso de «Mixed Content» en consola.

#### Las fotos de los anuncios no eran de coches

Los 48 anuncios sembrados pedían su imagen a `picsum.photos`, que devuelve fotografías
**aleatorias**: paisajes, personas y objetos sin relación con el vehículo. Un Toyota Hilux
podía salir ilustrado con una montaña.

Corregido: 16 fotografías reales de Wikimedia Commons, dos por modelo, recortadas a 3:2
(1280×853 y miniatura de 480×320) y servidas desde los estáticos del frontend —no desde
`uploads/`, que se pierde en cada reinicio de Render—. La procedencia y las licencias
quedan en `frontend/src/assets/vehicles/CREDITS.md`.

Cambiar el sembrador no bastaba, porque los anuncios ya estaban en la base de datos: el
reseeder gana un paso que **sustituye solo las imágenes que siguen apuntando a picsum**,
así que es idempotente y nunca pisa una foto subida por una persona.

#### Corregido en el mismo pase

- **El listado de negociaciones del backoffice contaba 12 y enseñaba 1** (§12.7).
- Ambas correcciones llevan pruebas: 511 en verde, sin cambios de modelo pendientes.

#### Verificado funcionando

- Suspender, bloquear y reactivar cuentas, siempre con motivo y con su fila en el journal
- Ocultar un anuncio **sin tocar** el estado del vendedor, y la imposibilidad de editar su
  información comercial
- La privacidad de las negociaciones de punta a punta: sin contenido en listado ni ficha,
  lectura con motivo y registro en la misma operación
- La cola de demandes completa, de `YD00001` a su cancelación por el usuario
- Modération de `SG00001` a su cierre, con la explicación exigida
- Comunicación a «Tous» que **excluye a la cuenta bloqueada** (9 de 10)
- Los rangos de Configuration, los catálogos y la inmutabilidad del código de equipamiento
- Puntos: ajuste sin motivo rechazado, `+50`/`−50` con los dos movimientos conservados
- Favoritos, alerta de **bajada de precio** (llegó la notificación `price-drop`) y el
  histórico de precios intacto
- Búsqueda guardada con su alerta, y `?userId=` ignorado en todos los endpoints probados

#### Tercer frente: las páginas legales

🔴 **Las cinco páginas legales describen una sociedad española y citan leyes españolas y
europeas.** Título en francés, cuerpo en español: «Yoon U Auto, **S.L.**», domicilio en
**Madrid**, NIF de relleno `B-12345678`, Registro Mercantil de Madrid, y la **Ley 34/2002**
como base del aviso legal. La de RGPD invoca el Reglamento (UE) 2016/679 y la Ley Orgánica
3/2018. Están **enlazadas desde el pie de todas las páginas**.

No es un problema de traducción: es que el aviso legal de un marketplace senegalés declara
una empresa que no es la suya, en un país que no es el suyo, bajo una ley que no le aplica.
Hay que reescribirlas con los datos reales y la normativa de Senegal, no traducirlas.

#### Lo que ya no hace falta esperar

Dos puntos del apartado 12.2 se han podido cerrar sin cronómetro:

- El **recordatorio por kilometraje** salta en el acto al actualizar el contador, con su
  notificación. Solo queda sin ver el disparo autónomo del trabajo de las 6 h.
- La **alerta de bajada de precio** llegó al instante.

Y uno se ha aclarado sin poder cerrarlo: la **alerta de búsqueda guardada** no saltó
porque el servicio excluye a quien publica de su propia alerta
(`NewVehicleAlertService.cs:27`). Es lo correcto, pero deja el camino completo sin probar.

#### Decisiones pendientes

- [ ] **Retirar un equipamiento lo borra de los anuncios que ya lo tenían.** Al retirar
      «Climatisation», el anuncio `YU10025` dejó de mostrarla; al reactivarla, volvió. La
      fila de enlace no se borra —la ficha filtra por `IsActive`—, pero retirar una
      entrada del catálogo cambia lo que dicen los anuncios ya publicados. ¿Es lo que se
      quiere, o solo debería desaparecer del formulario de publicación?
- [ ] **El administrador ve los anuncios no públicos dentro del Marketplace público.** La
      portada le anuncia «49 véhicules disponibles» cuando hay 46: el listado le cuela los
      pausados, el vendido y el ocultado. `/vehicles/count` sí devuelve 46. ¿Debería el
      backoffice ser el único sitio donde se ven?
- [ ] **«Jusqu'à trois véhicules» está escrito a mano** en `/mis-busquedas` con el límite
      configurado en 4. El resto de la aplicación lee el ajuste

### 12.11 Barrido de enlaces en busca de 404 (2026-08-16)

Recorrido de toda la navegación en producción, como visitante y como administrador,
recogiendo cada `href` interno y visitando después uno por uno los destinos.

| Comprobación | Resultado |
|---|---|
| Enlaces internos distintos recogidos | **40** en 10 páginas públicas y de usuario, **28** en 7 del backoffice |
| Rutas visitadas | **53** (públicas, de usuario y `/admin/*`) |
| 404 reales | **0** — ninguna ruta enlazada está rota |
| Rutas retiradas que deben dar 404 | **17/17 correctas**: las 14 públicas del producto anterior (`/precios`, `/tramitacion`, `/transporte`, `/financiacion`, `/inspectores`, `/guias/*`, `/concesionarios`, `/tracking`, `/pagos`, `/valoraciones`, `/calculadora`, `/logistica`) y las 3 de administración (`/admin/procesos`, `/admin/incidencias`, `/admin/partners`) |
| Enlaces que aún apuntaran a lo retirado | **ninguno** — el menú, el pie y la barra lateral del backoffice quedaron limpios tras el borrado |

Las 13 rutas que redirigen a `/auth/login` sin sesión lo hacen por el `authGuard`: es el
comportamiento esperado, no un enlace roto.

**Lo que sí apareció, y va corregido en esta misma rama:**

| Hallazgo | Corrección |
|---|---|
| La propia página de 404 estaba **en español** («Página no encontrada», «Volver al inicio») y con el león del producto anterior | Reescrita en francés, con dos salidas (ver vehículos / volver al inicio) en vez de una, y `OnPush` |
| El buzón vacío decía «No tienes mensajes aún» | En francés |
| Las 5 preguntas rápidas del chat estaban en español, y una preguntaba por la **exportación** —concepto del producto anterior— | En francés; la última pasa a preguntar por el **estado aduanero**, que es lo que importa en Senegal |
| `vehicles-placeholder.component.ts` y `coming-soon/` seguían en el repositorio con texto en español | **Eliminados**: nadie los referenciaba desde el borrado del legacy, eran código muerto |

Un rastreo posterior de caracteres y vocabulario propios del español por todo el frontend
no encontró **ningún otro** texto visible sin traducir.

> Único error de consola durante el barrido: un `Failed to start the connection` de SignalR,
> provocado por mi propia navegación al abortar la negociación. No es un fallo de la aplicación.

### 12.12 Tiempo real con dos identidades (2026-08-16)

Montaje: la aplicación real en el navegador como **QA Particulier** (`+221771234501`,
vendedor), y **QA Professionnel** (`+221771234502`, comprador) actuando desde fuera. Como
dos pestañas del mismo navegador comparten `localStorage` y no pueden tener dos sesiones,
el segundo usuario se conectó al hub por **WebSocket crudo**, hablando el protocolo de
SignalR a mano. Anuncio de apoyo: **YU10029**, negociación `b08596a5`.

**El servidor funciona. Entrega los tres eventos:**

| Invocación de B | Recibido por A | |
|---|---|---|
| `SendMessage` | `ReceiveMessage` con cuerpo, emisor y fecha | ✅ |
| `StartTyping` | `UserTyping` | ✅ |
| `MarkAsRead` | `MessageRead` con `readAt` | ✅ |

Además `MessageSent` vuelve al emisor. La autenticación por WebSocket con el token en la
query funciona, y el hub reparte por `Clients.User(...)`, así que llega esté donde esté
la otra parte.

**En pantalla, el resultado depende de cuál de las dos se mire:**

| | `/mensajes/:id` | `/mis-negociaciones/:id` |
|---|---|---|
| Mensaje entrante sin recargar | ✅ aparece | ❌ **nada** |
| Indicador «está escribiendo» | ✅ aparece y se va a los 3 s | ❌ nada |
| Acuse de lectura | ✅ pasa de ✓ a ✓✓ | ❌ nada |

🔴 **La pantalla de la Etapa 2 no tiene tiempo real.** Y es la que manda la
especificación: la negociación es el agregado raíz y el chat cuelga de ella. Son dos
fallos que se suman:

1. `negotiation-detail.component.ts` **no se suscribe a nada**. Llama a
   `joinConversation()` —que lo mete en un grupo del hub— pero no escucha
   `incomingMessage`, `typingNotification` ni `readReceipt`. Ese `JoinConversation` no
   sirve de nada: el hub reparte por usuario, no por grupo.
2. Envía con `sendMessageRest()`, y **`SendMessageCommandHandler` no empuja nada**: guarda
   el mensaje y devuelve. El camino que sí avisa es `SendMessage` del hub, que solo usa la
   pantalla antigua.

Medido: mensaje enviado por B, **9 segundos** sin que la pantalla de A se inmutara; tras
recargar, ahí estaba. O sea que se guarda bien y lo que falla es el aviso.

🔴 **Un mensaje nuevo tampoco genera notificación.** La campana de A siguió a **cero**.
La categoría `NotificationCategories.Message` existe, pero el único sitio que la usa es la
respuesta del administrador a una *demande*. Entre particulares no se notifica nada: ni en
vivo, ni en la campana, ni por correo —que además está sin configurar—. Quien recibe un
mensaje solo se entera si vuelve a entrar y mira.

**Textos en español encontrados aquí** (ya corregidos): «Cargando mensajes...», «Escribe
un mensaje...», «Enviar mensaje», los rótulos «Leído» y «Enviado» de los tics, «El usuario
está escribiendo…», el título «Mensajes» del buzón, el mensaje de error al procesar un
documento y **las nueve respuestas del interceptor global de errores** —que son las que ve
cualquiera ante cualquier fallo HTTP de toda la aplicación—.

> ⚠️ El barrido de §12.11 no los vio porque buscaba etiqueta y texto en la misma línea, y
> casi todo el marcado los tiene en líneas distintas. Repetido sin esa limitación.

### 12.13 Tiempo real en la negociación, ya corregido (2026-08-16)

Aplicada la opción 1: el tiempo real se lleva a la pantalla de la negociación y se retira
el buzón duplicado. **Vuelto a probar en producción con el mismo montaje de dos
identidades, sobre la negociación `b08596a5` (YU10029):**

| Comprobación | Resultado |
|---|---|
| Mensaje de B, sin tocar la pantalla de A | ✅ **aparece solo** |
| Notificación en la campana de A | ✅ categoría `message`, «Nouveau message», con el texto y enlace a `/mis-negociaciones/{id}` |
| «QA Professionnel est en train d'écrire…» | ✅ aparece y se apaga sola a los 3 s |
| Acuse de lectura del mensaje de A | ✅ pasa de ✓ «Envoyé» a ✓✓ «Lu» |
| `/mensajes` y `/mensajes/:id` | ✅ redirigen a `/mis-negociaciones` y al hilo |

El aviso sale ahora de **donde se guarda el mensaje**, no del transporte por el que llegó
la petición, así que llega igual por el camino que sea.

> Dos apuntes de método, por si sirven más adelante:
> - Render tardó más que Vercel. Para saber si ya estaba la API nueva sin escribir nada en
>   la base de datos, valió invocar el método `SendMessage` del hub —que la versión nueva
>   ya no tiene—: contestó «Method does not exist».
> - Un intento intermedio dio «la campana sigue a cero» y era **mío**: la respuesta de
>   `/notifications` es `{unreadCount, items}` y yo la leía como si viniera envuelta en
>   `value`. La notificación estaba desde el principio.

### 12.9 Limpieza pendiente de las pruebas

Estas cuentas y datos los he creado yo probando. **Conviene retirarlos antes de abrir al
público:**

| Qué | Detalle |
|---|---|
| `+221770000101` | «Test Vendeur QA» — **es el administrador actual** (`Seed:AdminPhone`) |
| `+221770000202` | «Test Acheteur QA» |
| Anuncio `YU10025` | Toyota Hilux 2020, ya vendido |
| Contrato `YC00001` | Venta verificada de prueba, con su PDF y su QR |
| 48 anuncios sembrados | Catálogo senegalés de demostración |
| 6 cuentas de vendedor | `+221771000001` … `+221771000006`, sin contraseña utilizable |
| Anuncio `YU10029` | «Toyota Corolla 2017 — recette temps réel», de QA Particulier. Creado para §12.12 |
| Negociación `b08596a5` | Su hilo con QA Professionnel, con los mensajes de la prueba y sus notificaciones |
| ⚠️ 11 negociaciones huérfanas | Del seed anterior, apuntan a anuncios retirados (pendiente nº 23) |

Añadido en la segunda sesión (16/08/2026):

| Qué | Detalle |
|---|---|
| Marca `Kia` y modelo `Sportage` | Creados para probar el rechazo de duplicados |
| Modelo `Sportage` en `Seat` | Creado para probar que el mismo nombre vale en otra marca |
| Equipamiento `SIEGES_CHAUFF` | «Sièges chauffants (renommé)», creado para probar que el código no cambia |
| Signalement `SG00001` | Sobre `YU00043`, cerrado como «Résolu» |
| Demande `YD00001` | Cancelada, con una propuesta externa a `example.com` |
| Comunicación «Test de recette» | Enviada a los 9 usuarios no bloqueados |
| Anuncio `YU00042` | Precio bajado de 4.700.000 a 4.400.000 para probar la alerta. **No revertido a propósito**: revertirlo añadiría otra fila al histórico, que es inmutable |
| Petición de corrección sobre `YU00039` | Y ese anuncio sigue **ocultado** desde las 11:48 |
| Vehículo de Mon Garage | Toyota Land Cruiser 2018, con 2 recordatorios, 1 intervención, 1 documento (PDF de 193 o) y 1 foto de 79 o. **La transparencia quedó desactivada del todo** |
| Anuncio `YU10026` | Creado por «Vendre ce véhicule». Se le puso precio y se publicó para probar, y **quedó archivado**: no está en el escaparate |
| Anuncio `YU10027` | Duplicado de `YU00042` para probar «Dupliquer». **Archivado** |
