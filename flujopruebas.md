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

- [ ] **Rappels de Mon Garage**: el trabajo en segundo plano corre cada 6 h. Crear uno
      que venza hoy y comprobar que llega la notificación
- [ ] **Alerta de bajada de precio**: bajar el precio de un favorito y ver la notificación
- [ ] **Alerta de búsqueda guardada**: publicar un anuncio que encaje y comprobar que
      salta **una sola vez**, y que no vuelve a saltar al reactivar tras una pausa
- [ ] **Caducidad del token**: dejar la sesión abierta más de 15 minutos y seguir
      navegando sin que se cierre (corregido, pero conviene confirmarlo en uso real)

### 12.3 Etapa 1 — sin probar

- [ ] Favoritos: marcar, desmarcar, y que el contador del anuncio cambie
- [ ] Búsquedas guardadas: guardar desde el buscador, activar y desactivar su alerta
- [ ] Comparador: añadir hasta 3, que el cuarto avise, y que **cambiar el límite en
      Configuration lo cambie sin desplegar**
- [ ] «Trouvez-moi cette voiture»: crear una demande, ver su `YD#####`, cancelarla
- [ ] Que el **teléfono del vendedor esté oculto** para quien no tiene cuenta

### 12.4 Etapa 2 — sin probar

- [ ] **Pedir una modificación** del contrato, corregirlo y reenviarlo
- [ ] **Anular** un contrato y comprobar que se puede crear otro en la misma negociación
      (índice único parcial `contracts.negotiation_id`, que los tests no validan)
- [ ] Plantillas de respuesta rápida del vendedor
- [ ] Rechazar una oferta

### 12.5 Mon Garage — sin probar

- [ ] **Rappels**: crear por fecha y por kilometraje, y que el de kilómetros solo salte
      al actualizar el contador
- [ ] Que el **kilometraje no pueda bajar**
- [ ] **Transparence**: compartir una intervención y comprobar que **su factura no se
      comparte sola** — son dos casillas
- [ ] Ver lo compartido en el anuncio público desde una sesión cerrada
- [ ] **«Vendre ce véhicule»**: que salga un borrador **sin precio, sin estado aduanero
      y sin descripción**
- [ ] Fotos del vehículo (⚠️ se perderán al reiniciar Render, pendiente nº 2)

### 12.6 Mes annonces — sin probar

- [ ] Los seis estados y sus cambios desde la lista
- [ ] Estadísticas por anuncio: visitas, favoritos, contactos
- [ ] Score de calidad del anuncio
- [ ] Duplicar y archivar
- [ ] Publicar cinco anuncios seguidos: **sin límite**

### 12.7 Backoffice — la mayor parte sin probar

Verificado: tableau de bord y statistiques. Falta:

- [ ] **Utilisateurs**: suspender con fecha, bloquear, notas internas, y que **toda
      medida exija motivo**
- [ ] **Annonces**: ocultar con `AdminHiddenAt` sin tocar el estado del vendedor, y que
      el administrador **no pueda editar** título, precio ni descripción
- [ ] **Demandes**: asignarse, proponer vehículo interno y externo, responder
- [ ] ⚠️ **Négociations**: que el contenido **no aparezca** en el listado ni en la ficha,
      que leerlo **exija motivo**, y que esa lectura quede registrada en el journal
- [ ] **Contrats**: invalidar con motivo, que baje el contador de ventas verificadas y
      **que no exista forma de validar** en nombre de las partes
- [ ] **Modération**: reportar, referencia `SG#####`, cerrar exigiendo explicación
- [ ] **Communications**: enviar por audiencia y región; ⚠️ **no saldrá ningún correo**
      (pendiente nº 3)
- [ ] **Configuration**: parámetros fuera de rango rechazados, interruptores, catálogos,
      y que el **código de un equipamiento no cambie**
- [ ] **Journal**: valor anterior y nuevo, filtros, y que **no se pueda editar ni borrar**
- [ ] **Points**: ajuste manual sin motivo rechazado *(el +100 automático ya está
      verificado)*

### 12.8 Transversal

- [ ] ⚠️ Que **ningún endpoint acepte `userId` por query string**: añadir
      `?userId=<otro>` a una llamada y comprobar que se ignora
- [ ] Repaso de idioma pantalla por pantalla: quedan zonas del producto anterior sin
      traducir, y las nuevas conviene mirarlas con ojos frescos
- [ ] Que ninguna pantalla tarde más de 3 segundos con datos reales
- [ ] **Statistiques con volumen**: las agregaciones se hacen en memoria (pendiente
      nº 19); medir cuánto tardan cuando haya miles de filas

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
| ⚠️ 11 negociaciones huérfanas | Del seed anterior, apuntan a anuncios retirados (pendiente nº 23) |
