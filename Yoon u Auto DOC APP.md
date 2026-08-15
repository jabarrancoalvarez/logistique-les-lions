

## YOON U AUTO
## Functional & Product Specification
Versión 1.0 (MVP)
- Visión del producto
Yoon u Auto es un ecosistema digital para la compra, venta y gestión de vehículos en
Senegal. El objetivo del MVP es captar el mayor número posible de usuarios y vehículos
mediante una plataforma gratuita, sencilla y segura.
- Principios del producto
- Durante el registro los campos que se almacenan: Teléfono, Nombre, Tipo de usuario
(Particular o Profesional), Ciudad, Fecha de registro (automático) y ID usuario
## (automático).
- Todas las funcionalidades para usuarios son gratuitas.
- 'Pide tu vehículo' es una funcionalidad estratégica y gratuita para cualquier usuario.
- Todo el frontend y todos los textos visibles para el público se implementarán en francés.
- El usuario podrá buscar, comprar, vender, pedir vehículos y gestionar Mi Garaje
gratuitamente.

- Actores del sistema (tipos de cuenta)
- Visitante: Persona no autenticada. Puede consultar, buscar, filtrar, ordenar, comparar y
compartir anuncios, pero no puede realizar acciones protegidas. No puede ver el
teléfono del usuario que vende por ejemplo.
- Usuario: Cuenta general autenticada. Puede comprar, vender, gestionar vehículos,
publicar anuncios, chatear, ofertar, crear solicitudes, gestionar contratos y utilizar Mi
## Garaje.
- Administrador: gestiona la plataforma.
- Experiencia del usuario
Etapa 1 - Encuentra tu vehículo
Objetivo: ayudar al usuario a localizar el vehículo adecuado y generar demanda dentro de la
plataforma. Las funcionalidades que se deben desarrollar son:
- Marketplace con listado de vehículos.

El Marketplace constituye la pantalla principal de descubrimiento de vehículos.
Los anuncios se mostrarán mediante tarjetas optimizadas para móvil, evitando obligar
al usuario a abrir cada anuncio para conocer sus características esenciales. En este
listado general de anuncios, cada tarjeta mostrará:


- Fotografías subidas. Poder verlas y deslizarlas.
- Marca y modelo.
- Versión, cuando exista.
- Tipo de combustible
## • Año.
## • Kilometraje.
## • Ciudad.
- Precio en CFA.
- Indicador de precio, cuando haya datos suficientes.
- Icono para añadir a Favoritos. (si un visitante pulsa este icono, debe salir la pantalla
de regístrate)
- Icono para poder compartir el anuncio por whatsapp, e-mail, copiar enlace, otras
redes sociales.
- Icono de comparar. Para comprar vehículos (si un visitante pulsa este icono, debe
salir la pantalla de regístrate)

## Ordenación
Se podrá ordenar los resultados por:
- Más recientes.
- Precio: menor a mayor.
- Precio: mayor a menor.
- Kilometraje: menor a mayor.
- Año: más nuevos primero.

- Buscador y filtros avanzados.

Debe existir una barra de búsqueda sencilla acompañada de filtros.
Barra de búsqueda
Que permita buscar directamente: Toyota Corolla o cualquier palabra-

Filtros principales
Siempre visibles o accesibles en un solo toque:
## Marca
## • Toyota
## • Peugeot
## • Renault
- Mercedes-Benz
## • Hyundai
## • Kia
## • Ford
## • Citroën
## • Nissan

## • BMW
- etc.
## Modelo
Dependiente de la marca seleccionada.
Toyota → Corolla, RAV4, Hilux, Prado, Yaris...
## Precio
- Precio mínimo.
- Precio máximo.
- Valores en FCFA.
## Año
## • Desde.
## • Hasta.
## Kilometraje
## • Desde.
## • Hasta.
## Ubicación
## • Región.
## • Ciudad.
Por ejemplo: Dakar, Thiès, Mbour, Saint-Louis, Ziguinchor, Diourbel, etc.
Estado aduanero
## • Dédouané.
- Non dédouané.
## • Passavant.
Este filtro debe tener especial visibilidad por su importancia en Senegal.
## Combustible
## • Diesel.
## • Essence.
## • Hybride.
- Hybride rechargeable.
## • Électrique.
## • Autre.
## Cambio
## • Manuel.
## • Automatique.
## Carrocería
## • Citadine.
## • Berline.
## • Break.
- SUV / 4x4.
## • Coupé.
## • Cabriolet.
## • Monospace.
## • Pick-up.

## • Fourgon / Utilitaire.
## Potencia
## • Desde.
## • Hasta.
## Cilindrada
## • Desde.
## • Hasta.
## Tracción
## • Delantera.
## • Trasera.
- 4x4 / AWD.
## Puertas
## • 2/3.
## • 4/5.
## Plazas
## • 2, 3, 4, 5, 7, 8+.
## Color
Equipamiento, mediante selección múltiple:
## • Climatisation.
## • Bluetooth.
- Navigation/GPS.
- Caméra de recul.
- Radar de stationnement.
- Toit ouvrant.
- Intérieur cuir.
## • ISOFIX.
- Phares LED.
- Régulateur de vitesse.
- Jantes alliage.
- etc.
Tipo de usuario que publica
## • Todos.
## • Particular.
## • Profesional.
## Botones
Appliquer les filtres
Effacer les filtres
Otro botón que permita guardar búsqueda y se guarde en la sección Mis
búsquedas. Si el usuario no está registrado, al pulsar este botón debe llevarle a
registrarse.

Y siempre debe indicarse cuántos resultados producen los filtros antes o
inmediatamente después de aplicarlos.



- Ficha completa del vehículo / Détail du véhicule
Cuando un visitante o usuario pulsa sobre un anuncio del Marketplace, se abrirá la ficha
completa del vehículo. Esta pantalla debe concentrar toda la información publicada
sobre el coche y todas las acciones disponibles para avanzar hacia una posible compra.
No será necesario estar registrado para consultar la ficha completa. El registro se
solicitará únicamente al utilizar determinadas funciones como Favoritos, Comparar,
Chat, Hacer una oferta o Contactar por WhatsApp.
La ficha se organizará en los siguientes bloques.
- Galería de fotografías
Será el primer elemento visual de la ficha.
Debe permitir:
- Mostrar todas las fotografías publicadas.
- Deslizar horizontalmente entre fotografías.
- Pulsar una fotografía para verla a pantalla completa.
- Ampliar la fotografía.
- Mostrar el número de fotografías disponibles: 3 / 12.
- Navegar hacia delante y atrás.
- Compartir el anuncio.
- Añadir a Favoritos.
- Añadir al Comparador.
En móvil, la fotografía debe ocupar buena parte del ancho de pantalla.
Si el visitante pulsa Favoritos o Comparar sin estar registrado, aparecerá el proceso de
registro/inicio de sesión.
- Información principal del vehículo
Inmediatamente debajo de las fotografías debe aparecer la información esencial:
## Ejemplo:
Toyota RAV4 2.0 VVT-i
## 8.900.000 FCFA
2019 · 126.000 km · Essence · Automatique
## Dakar
Y, cuando exista información estadística suficiente:
Bonne affaire,     Prix correct o     Prix élevé
El indicador podrá acompañarse de una explicación breve:
Prix calculé à partir de véhicules similaires disponibles sur Yoon u Auto.
Si no existen suficientes datos comparables, no se mostrará ningún indicador
inventado.
- Características del vehículo
Debe existir un bloque claramente identificado como:
## Caractéristiques
Mostrará todos los datos técnicos disponibles.
Información general

## • Marca.
## • Modelo.
## • Versión.
## • Año.
## • Kilometraje.
## • Combustible.
- Caja de cambios.
## • Carrocería.
## • Color.
- Número de puertas.
- Número de plazas.
Motor y características técnicas
## • Potencia.
## • Cilindrada.
## • Tracción.
- Motorización, cuando proceda.
Si un campo opcional no ha sido cumplimentado, puede omitirse de la ficha en lugar de
llenar la pantalla de Non renseigné.
- Estado aduanero
Por su importancia en Senegal, debe constituir un bloque claramente visible y no quedar
escondido entre las características técnicas.
Statut douanier
El anuncio mostrará uno de los siguientes estados:
## •     Dédouané
-     Non dédouané
## •     Passavant
Podremos acompañarlo posteriormente de información explicativa para usuarios que
desconozcan el significado de cada estado.
El estado declarado pertenece a la información introducida por el usuario que publica el
vehículo. En la V1, Yoon u Auto no debe presentarlo como información verificada
por la plataforma salvo que realmente implementemos posteriormente un
procedimiento de verificación documental.
Esto es importante para no transmitir una garantía que no estamos realizando.
## 5. Equipamiento
## Bloque:
## Équipements
Se mostrarán únicamente los equipamientos seleccionados por quien publicó el
vehículo.
Por ejemplo:
## • ✓ Climatisation.
## • ✓ Bluetooth.
- ✓ Navigation / GPS.

- ✓ Caméra de recul.
- ✓ Radar de stationnement.
- ✓ Régulateur de vitesse.
- ✓ Toit ouvrant.
- ✓ Intérieur cuir.
## • ✓ ISOFIX.
- ✓ Phares LED.
- ✓ Jantes alliage.
- Descripción del vehículo
## Bloque:
Description du vendeur
Aquí se mostrará íntegramente el texto introducido al publicar el anuncio.
Por ejemplo, podrá contener información sobre:
- Estado general.
- Mantenimiento realizado.
- Reparaciones recientes.
- Estado de neumáticos.
- Motivo de venta.
- Defectos conocidos.
- Accesorios incluidos.
- Cualquier otra información relevante.
Yoon u Auto no modificará ni generará mediante IA esta descripción.
- Información sobre el usuario que publica
Debe aparecer un bloque específico:
Vendu par
Podrá contener:
- Nombre o nombre comercial mostrado.
## • Particular / Professionnel.
## • Ciudad.
- Fecha de alta en Yoon u Auto.
- Teléfono verificado.
- Número de anuncios activos.
- Número de ventas verificadas, cuando existan.
## Ejemplo:
## Auto Dakar Services
## Professionnel
Membre depuis 2026
✓ Téléphone vérifié
14 annonces · 8 ventes vérifiées
## O:
## Mamadou Diop
## Particulier

Membre depuis 2025
✓ Téléphone vérifié
2 ventes vérifiées
También se podrá pulsar sobre el usuario para consultar sus otros vehículos publicados.
## 8. Ubicación
## Bloque:
## Localisation
Debe mostrar:
## • Región.
## • Ciudad.
- Zona/barrio, si decidimos recoger ese dato en la publicación.
Por seguridad y privacidad, no se mostrará la dirección exacta del vehículo.
Podemos mostrar una localización aproximada mediante mapa si posteriormente
consideramos que aporta valor, pero no es imprescindible para el MVP.
- Acciones principales
Esta es una de las partes más importantes de la ficha.
Especialmente en móvil, las acciones principales deben permanecer fácilmente
accesibles.
Ajouter aux favoris
Guarda el anuncio en:
Mi Espacio →      Mes recherches → Favoris
Si es visitante:
Connectez-vous pour ajouter ce véhicule à vos favoris.
## Comparer
Añade el anuncio al comparador.
Máximo tres vehículos.
Se almacenará en:
Mi Espacio →      Mes recherches → Comparateur
Si es visitante, deberá registrarse.
Faire une offre
Permite realizar una oferta económica e iniciar una conversación interna asociada
específicamente a ese anuncio. Si no está registrado, deberá registrarse.
El chat debe conservar siempre la referencia del vehículo.
Al pulsarlo se abre un pequeño formulario:
Prix affiché: 8.900.000 FCFA
Votre offre: [________] FCFA (optionnel)
## Message (optionnel)
Envoyer l'offre
El usuario que publicó el anuncio recibirá la oferta en Mi Espacio → Mis ofertas.
Si el visitante no está registrado, primero deberá registrarse.

## Partager

Esta función no requiere registro.
## Permitirá:
- WhatsApp.
## • E-mail.
- Copiar enlace.
- Compartir mediante las opciones disponibles en el dispositivo.
- Solicitar información adicional
Me gusta mantener esta funcionalidad porque puede diferenciar bastante la experiencia
sin añadir complejidad ni IA.
Dentro de la ficha aparecerá:
Besoin de plus d'informations ?
Con acciones rápidas como:
- Demander des photos supplémentaires.
- Demander une photo du moteur.
- Demander une photo de l'intérieur.
- Demander une photo du VIN.
- Demander une vidéo du véhicule.
- Demander une vidéo du moteur au démarrage.
Al pulsar una opción se abrirá el chat con un mensaje ya preparado.
Por ejemplo:
Bonjour, pouvez-vous m'envoyer une photo du moteur ?
Requiere estar registrado.
- Historial de precio del anuncio
Como necesitamos almacenar los cambios de precio para las alertas de Favoritos,
podemos aprovechar esa información dentro de la propia ficha.
No hace falta convertirlo en un gráfico complejo en la V1.
Podemos mostrar simplemente:
Évolution du prix
Prix actuel: 8.900.000 FCFA
Prix initial: 9.500.000 FCFA
## ↓ 600.000 FCFA
Y eventualmente:
Prix réduit le 7 août 2026.
Esto aumenta la transparencia y además reutiliza datos que ya necesitamos almacenar.
- Anuncios similares
Al final de la ficha mostraría:
Véhicules similaires
Por ejemplo, entre 4 y 8 vehículos relacionados mediante reglas normales de base de
datos, sin IA.
## Prioridad:
- Misma marca + modelo.
- Rango de precio similar.
- Año similar.

## 4. Ubicación.
- Si no hay suficientes, misma categoría/carrocería.
Cada resultado utilizará las mismas tarjetas reducidas del Marketplace.
Esto evita un callejón sin salida: si el coche no convence al usuario, puede continuar
buscando inmediatamente.
- Estado del anuncio
La ficha deberá reaccionar al estado del anuncio.
Disponible: todas las acciones activas.
Réservé: se informa claramente; podremos decidir si permitimos seguir contactando.
Vendu: la ficha puede continuar existiendo por referencias, favoritos, comparaciones o
contratos, pero debe mostrar:
Ce véhicule a été vendu.
Y desactivar:
- Faire une offre.
## • Contacter.
- Chat para nuevos interesados.
Se podrán seguir mostrando vehículos similares.
- Identificación y referencia
Cada anuncio tendrá una referencia pública única:
Réf. Yoon: #YU12345
Debe aparecer discretamente en la ficha y utilizarse en:
## • Chat.
## • Ofertas.
## • Contratos.
## • Administración.
## • Soporte.
Así tenemos un identificador comprensible para el usuario sin exponer necesariamente
el UUID interno de la base de datos.
Flujo completo de la ficha
Quedaría conceptualmente:
Marketplace → Ficha del vehículo → Analizar información → Favorito / Comparar
/ Compartir → Chat / Solicitar información → Hacer oferta → Negociación →
## Contrato → Compra → Mi Garaje.
Y esto conecta perfectamente la Etapa 1 "Encuentra tu coche" con la Etapa 2 "Me
interesa / quiero verlo / negociar" que definimos al principio.


- (icono lupa) Mis recherches. Es un apartado importante del menú de usuario.
Dentro tendría cuatro pestañas o bloques:
Favoris · Recherches enregistrées – Comparador – Mis pedidos
## 1. Favoris
Aquí aparecen vehículos concretos que el usuario está siguiendo.
Desde allí podrá:

- Abrir el anuncio.
## • Eliminarlo.
## • Compararlo.
## • Compartirlo.
- Contactar con quien lo publica.
El favorito debe mantener la referencia al anuncio, no crear una copia.
Si el precio cambia, el favorito mostrará el precio actualizado.
Si el vehículo se vende, aparecerá como Vendu en lugar de desaparecer.

En esta sección, arriba, un selector que indique que todos los vehículos guardados en
favoritos recibirán alertas de si baja el precio. Si quita la selección, que el usuario pueda
poner las alertas a los vehículos favoritos que quiera.
Y esto conecta directamente con las alertas. Por ejemplo, si guardé un RAV4 por
9.500.000 FCFA y baja a 8.900.000 FCFA, no necesito ir a otra sección llamada Alertas.
Ya se puede ver la tarjeta del vehículo y la bajada de precio. Además haber recibido la
correspondiente notificación por e-mail.

- Recherches enregistrées
Aquí no sigo un coche concreto, sino un tipo o vario de coche.
Por ejemplo:
## Toyota Hilux
2017–2022 · ≤150.000 km · ≤12.000.000 FCFA · Dakar
23 véhicules disponibles
Cada búsqueda guardará exactamente los filtros utilizados:
## • Marca.
## • Modelo.
## • Versión.
- Precio mínimo/máximo.
- Año mínimo/máximo.
## • Kilometraje.
## • Combustible.
## • Cambio.
## • Carrocería.
- Estado aduanero.
## • Ubicación.
- Resto de filtros avanzados seleccionados.
Y tendrá:
Voir les résultats · Modifier · Supprimer
## Además:
Alerte nouveaux véhicules: ON/OFF
Por tanto, la alerta no es realmente una entidad que el usuario tenga que gestionar
independientemente. Es una propiedad de la búsqueda guardada.
Eso simplifica bastante el producto.


## 3. Comparador
El comparador permite al usuario analizar hasta tres vehículos simultáneamente
para facilitar la decisión de compra. Los vehículos seleccionados deben mostrarse en
una misma pantalla, colocando sus características en paralelo para que las diferencias
sean fáciles de identificar.

El usuario podrá añadir vehículos al comparador desde:
## • El Marketplace.
- La ficha de un vehículo.
- Mes recherches → Favoris.
Cuando seleccione Comparer, el vehículo se añadirá al comparador. El sistema
mostrará en todo momento cuántos vehículos hay seleccionados:
## Comparer (2/3)
Al alcanzar tres vehículos, para añadir otro será necesario eliminar previamente uno de
los seleccionados.
Información mostrada en la comparación
La comparación debe utilizar exclusivamente los datos existentes en los anuncios.
En la parte superior se mostrarán las tres tarjetas de los vehículos con:
- Fotografía principal.
- Marca, modelo y versión.
- Precio actual.
- Indicador de precio: Bonne affaire / Prix correct / Prix élevé, cuando exista
información estadística suficiente.
## • Ciudad.
- Estado del anuncio: Disponible / Réservé / Vendu.
Debajo se mostrará una tabla comparativa organizada por bloques.
Características principales:
## • Año.
## • Kilometraje.
## • Combustible.
## • Cambio.
## • Carrocería.
## • Potencia.
## • Cilindrada.
## • Tracción.
- Número de puertas.
- Número de plazas.
## • Color.
Situación administrativa:
- Estado aduanero: Dédouané / Non dédouané / Passavant.
## Equipamiento:

Se mostrarán en paralelo los principales elementos declarados en cada anuncio, por
ejemplo:
## • Climatisation.
## • Bluetooth.
- GPS/Navigation.
- Caméra de recul.
- Radar de stationnement.
- Régulateur de vitesse.
- Toit ouvrant.
- Intérieur cuir.
## • ISOFIX.
- Jantes alliage.
## • Etc.
La interfaz deberá facilitar visualmente la identificación de diferencias, pero no
determinará automáticamente cuál de los tres vehículos es mejor.
Acciones disponibles
Desde el propio comparador, para cada vehículo, el usuario podrá:
- Voir l'annonce — abrir la ficha completa.
- Ajouter/Supprimer des favoris.
- Faire une offre.
- Contacter — iniciar chat o acceder al contacto por WhatsApp según las reglas de
autenticación establecidas.
## • Partager.
- Retirer du comparateur.
También podrá sustituir uno de los vehículos por otro sin necesidad de abandonar
completamente la comparación.
Relación con Favoris
Un vehículo no tendrá que estar en Favoris para poder compararse.
Son funciones diferentes:
- Favoris → vehículos que quiero seguir.
- Comparateur → vehículos que quiero analizar conjuntamente.
No obstante, desde Favoris debe resultar especialmente sencillo seleccionar varios
vehículos y pulsar:
Comparer la sélection
Si el usuario selecciona más de tres favoritos, el sistema deberá indicar:
Vous pouvez comparer jusqu'à 3 véhicules.
Persistencia del comparador
Para el usuario registrado, la selección del comparador puede conservarse entre
sesiones hasta que el usuario elimine los vehículos o sustituya la selección.
El comparador deberá almacenar únicamente las referencias (listing_id) de los anuncios
seleccionados. Nunca debe copiar los datos del vehículo, ya que precio, estado u otras
características pueden cambiar.

Por tanto, cada vez que se abra el comparador se consultarán los datos actuales del
anuncio.
Si cambia el precio, aparecerá el precio actualizado.
Si un anuncio pasa a reservado, aparecerá Réservé.
Si se vende, continuará temporalmente visible como Vendu, permitiendo al usuario
conservar la referencia de lo que estaba comparando, pero las acciones de oferta y
contacto quedarán desactivadas.
Comparación con datos incompletos
No todos los anuncios tendrán necesariamente todos los campos completos.
Si uno de los vehículos no tiene declarada una característica, el sistema mostrará:
Non renseigné
Nunca deberá inferir o inventar información.
Diseño móvil
Dado que Yoon u Auto será Mobile First, no debe intentarse comprimir tres columnas
completas en una pantalla pequeña.
En móvil, la primera columna con el nombre de la característica permanecerá
identificable y los vehículos podrán consultarse mediante desplazamiento horizontal,
manteniendo visibles sus cabeceras.
El objetivo es que comparar tres vehículos siga siendo cómodo desde un teléfono.
Regla de negocio
Su función es presentar de forma objetiva y estructurada la información de hasta tres
anuncios para que sea el propio usuario quien tome la decisión.

- Mes demandes — « Trouvez-moi cette voiture »
Mes demandes permite al usuario solicitar a Yoon u Auto la búsqueda de un vehículo
que no ha encontrado entre los anuncios disponibles, especialmente cuando está
interesado en importar un vehículo desde otro país.
La funcionalidad será gratuita y estará disponible para todos los usuarios registrados,
independientemente de que hayan indicado Particulier o Professionnel en su perfil.
La finalidad de esta función es doble: ofrecer al usuario una alternativa cuando no
encuentra el vehículo que busca y permitir a Yoon u Auto conocer la demanda real
existente en el mercado.
Crear una nueva solicitud
Dentro de Mes recherches → Mes demandes aparecerá de forma destacada el botón:
+ Trouvez-moi une voiture
Al pulsarlo se abrirá un formulario para definir el vehículo solicitado.
Datos del vehículo:
## • Marca.
## • Modelo.
- Versión, opcional.
- Año mínimo.
- Año máximo.
- Kilometraje máximo.

## • Combustible.
- Caja de cambios.
- Carrocería, cuando proceda.
- Color, opcional.
- Equipamiento o características especialmente importantes, opcional.
## Presupuesto:
- Presupuesto máximo en FCFA.
## Procedencia:
En esta V1, dado que queremos orientar esta función principalmente a la importación,
incluiría:
## • Importation
## • Sénégal
## • Indifférent
## Observaciones:
Campo de texto libre:
Précisez votre recherche
Aquí podrá indicar cuestiones como:
Toyota Hilux double cabine, diesel, automatique si possible. Je préfère un véhicule
européen avec moins de 120.000 km.
No habrá IA procesando esta información en la V1. Se almacena y se presenta al
administrador.
Envío de la solicitud
Al pulsar:
Envoyer ma demande
el sistema:
- Guarda la solicitud asociada al usuario.
- Le asigna una referencia única.
- Registra fecha y hora.
- Establece el estado inicial como Nouvelle demande.
- Envía una notificación al panel de administración.
El administrador verá, por ejemplo:
Nouvelle demande #YD-00248
Toyota Hilux · 2018–2022 · ≤120.000 km
Budget: 12.000.000 FCFA
## Origine: Importation
## Utilisateur: Mamadou Diop
Así que no es simplemente un formulario que manda un e-mail: la solicitud debe
convertirse en una entidad dentro de Yoon u Auto y poder gestionarse
posteriormente.
Seguimiento desde Mes demandes
El usuario verá todas las solicitudes realizadas.
Cada tarjeta podrá mostrar:
## Toyota Hilux

2018–2022 · ≤120.000 km
Budget: ≤12.000.000 FCFA
## Importation
État: En recherche
Réf. #YD-00248
Créée le 09/08/2026
De esta forma no necesita volver a enviar la misma petición ni preguntar qué ocurrió
con ella.
Estados de la solicitud
Yo utilizaría inicialmente:
Nouvelle demande
La solicitud acaba de enviarse.
En recherche
Yoon u Auto ha comenzado a gestionarla.
Véhicule proposé
Se ha encontrado al menos una posible opción.
## Terminée
La solicitud ha finalizado.
## Annulée
El usuario o el administrador la ha cancelado.
No complicaría más los estados en el MVP.
Propuesta de vehículo
Aquí hay una funcionalidad interesante que dejaría prevista desde el principio.
Cuando el administrador encuentre un coche que pueda encajar, podrá asociarlo a la
solicitud.
Puede tratarse de:
Un vehículo ya publicado en Yoon u Auto
o
Una propuesta externa de importación.
En el primer caso, el usuario podrá abrir directamente el anuncio.
En el segundo, inicialmente puede ser una propuesta básica introducida por el
administrador con:
## • Marca/modelo.
## • Año.
## • Kilómetros.
- Precio estimado.
- País de origen.
## • Fotografías.
## • Observaciones.
- Enlace externo, si procede.
El usuario recibe:
Nous avons trouvé un véhicule pour vous
y accede a su solicitud para consultar la propuesta.

Esto puede evolucionar muchísimo después, pero para el MVP basta con que exista la
estructura.
Comunicación con Yoon u Auto
Dentro de cada solicitud debería existir un pequeño hilo de comunicación entre usuario
y administrador.
No lo mezclaría con el chat entre usuarios asociado a los anuncios.
Son dos conversaciones diferentes:
Chat de anuncio: usuario ↔ usuario.
Mes demandes: usuario ↔ Yoon u Auto/administrador.
Así el administrador puede escribir:
Nous avons trouvé deux Toyota Hilux correspondant à votre recherche. Nous vous
envoyons les informations.
Y el usuario puede responder dentro de la solicitud.
## Cancelación
Mientras la solicitud no esté finalizada, el usuario podrá pulsar:
Annuler ma demande
Se solicitará confirmación.
La solicitud permanecerá en el histórico con estado Annulée; no debe borrarse
físicamente.
Panel del administrador
Cada nueva solicitud aparecerá en:
Administration → Demandes de véhicules
El administrador podrá:
- Consultar todos los criterios solicitados.
- Consultar los datos del usuario.
- Cambiar el estado.
- Añadir notas internas.
- Comunicarse con el usuario.
- Asociar vehículos de Yoon u Auto.
- Añadir una propuesta externa.
- Marcarla como finalizada.
- Consultar solicitudes anteriores del mismo usuario.
Las notas internas nunca serán visibles para el usuario.
## Notificaciones
El usuario recibirá una notificación cuando:
- Yoon u Auto comience a gestionar la solicitud.
- El administrador envíe un mensaje.
- Se encuentre un posible vehículo.
- Cambie de forma relevante el estado de la solicitud.
Para eventos importantes como Véhicule proposé, también podemos enviar correo
electrónico.




## •       Notificaciones
Esto sí debe existir globalmente.
El icono de campana de la aplicación reúne los acontecimientos:
Baisse de prix
La Toyota Hilux que vous suivez est passée de 9.500.000 à 8.900.000 FCFA.
Nouvelle annonce
3 nouveaux véhicules correspondent à votre recherche "Toyota Hilux Dakar".
Nouvelle offre
Vous avez reçu une offre de 7.500.000 FCFA.
Nouveau message
Contrat à valider
Rappel entretien
Es decir:
## Alerta ≠ Notificación.
Una alerta es una regla configurada por el usuario.
Una notificación es el evento que genera el sistema cuando esa regla —o cualquier otro
evento relevante— se produce.
Esto conviene dejarlo clarísimo en el PRD y en la base de datos.

Etapa 2 – Negocia y compra con confianza
Aquí agrupamos todo lo que sucede desde que existe interés real hasta que la
operación termina.
Y además esta etapa tiene su reflejo perfecto en:
Mes négociations
¿Qué es una negociación?
Una negociación comienza cuando un usuario realiza una acción que demuestra
intención real sobre un vehículo:
- Envía un mensaje.
- Solicita información adicional.
- Hace una oferta.
A partir de ese momento se crea una relación entre:
Usuario interesado ↔ Anuncio ↔ Usuario que publica
## Ejemplo:
Toyota RAV4 2019
## Mamadou ↔ Auto Dakar
## 8.900.000 FCFA
Négociation en cours
Y dentro de esa negociación estaría todo el historial de la operación.
Aquí habrá 3 bloques o pestañas.



## 1. Conversation
El chat interno asociado a un vehículo que ya hemos definido.
## Permitirá:
- Mensajes de texto.
## • Fotografías.
- Consultar el anuncio durante la conversación.
Este chat de Yoon u Auto sería el canal principal.
Dentro de la misma negociación o chat privado, debería aparecer la posibilidad de hacer
una oferta y ver de forma rápida en ese chat o negociación, el estado de oferta-
contraoferta:
Faire une offre
Precio publicado:
## 8.900.000 FCFA
## Oferta:
## 8.300.000 FCFA
La otra parte podrá:
Accepter · Refuser · Faire une contre-offre
Y todo queda dentro del mismo hilo de negociación.
Por ejemplo:
10:32 — Mamadou a envoyé une offre de 8.300.000 FCFA
11:14 — Contre-offre: 8.600.000 FCFA
11:27 — Mamadou a accepté l'offre.

- Mis Visitas / inspecciónes
Aquí pondría la checklist privada de inspección presencial asociada al vehículo en
negociación.
Porque esta sí que no pertenece a la búsqueda.
Pertenece a una negociación avanzada.
El usuario interesado puede utilizarla cuando vaya a ver el vehículo:
Inspection du véhicule
## • Motor.
## • Carrocería.
## • Neumáticos.
## • Interior.
- Aire acondicionado.
## • Luces.
## • Frenos.
## • Dirección.
## • Documentación.
## • VIN.
- Prueba de conducción.
- Observaciones personales.
Es completamente privada.

La otra parte no puede verla.
Y no constituye una certificación de Yoon u Auto.
- Mis contratos
Cuando se acepta una oferta o ambos usuarios acuerdan realizar la compraventa:
Contrat de vente
Desde la negociación:
Créer le contrat de vente
Muchos datos pueden precargarse automáticamente.
Del vehículo
## • Marca.
## • Modelo.
## • Versión.
## • Matrícula.
## • VIN.
## • Kilometraje.
- etc.
De la operación
- Precio acordado.
- Referencia del anuncio.
## • Fecha.
De los usuarios
Los datos disponibles de ambos perfiles.
Los datos legales que todavía falten —por ejemplo CNI/pasaporte— se solicitan en ese
momento.
Envío y validación
Una parte crea el contrato.
## ↓
La otra recibe:
Contrat à valider
## ↓
Revisa los datos.
## ↓
## Puede:
## Valider
o
Demander une modification
No utilizaría simplemente Rejeter para un error en el contrato. Es más natural permitir
solicitar correcciones.
Timeline de la negociación
Aquí creo que podemos hacer algo bastante bueno.
Toda la operación tendrá una cronología única:
8 agosto — 10:14
Conversation commencée

8 agosto — 10:32
Offre de 8.300.000 FCFA
8 agosto — 11:14
Contre-offre de 8.600.000 FCFA
8 agosto — 11:27
Offre acceptée
9 agosto — 09:40
Contrat créé
9 agosto — 10:05
Contrat validé
9 agosto — 10:06
Vente vérifiée ✓
Eso da una sensación de proceso muchísimo mayor.
Contrato definitivo
Cuando se valida:
## • Estado Validated.
- Generación del PDF.
- Referencia única.
- Código QR de verificación.
- Contrato disponible para ambas partes.
- Ya no puede modificarse.
- Queda almacenado en el histórico.
Venta verificada
Una vez finalizada correctamente:
Para quien vende
+1 vente vérifiée
Y los puntos de fidelización que definamos.
Para quien compra
El vehículo se incorpora automáticamente a:
## Mon Garage
Y empieza la siguiente etapa de Yoon u Auto.

Etapa 3 – Sigue usando Yoon u Auto
La relación del usuario con Yoon u Auto no debe finalizar cuando compra o vende un
vehículo.
La tercera etapa tiene como objetivo mantener al usuario dentro del ecosistema durante
todo el periodo en el que es propietario de un vehículo, ofreciéndole herramientas
gratuitas para organizar su información, controlar su mantenimiento, conservar
documentación, recibir recordatorios, conocer aproximadamente su valor de mercado y
preparar fácilmente una futura venta.
El apartado principal de esta etapa será:           Mon Garage
Mon Garage será el espacio privado del usuario para gestionar todos sus vehículos.

No estará limitado a vehículos comprados mediante Yoon u Auto. El usuario podrá
incorporar cualquier vehículo que ya posea, aunque lo haya comprado fuera de la
plataforma o antes de registrarse.
De esta forma, Yoon u Auto pasa de acompañar únicamente la compraventa a acompañar
todo el ciclo de vida del vehículo:
Comprar → Gestionar → Mantener → Conocer su valor → Vender → Comprar otro
vehículo
- Añadir vehículos
El usuario podrá incorporar vehículos a Mon Garage mediante dos vías.
A. Vehículo adquirido mediante Yoon u Auto
Cuando una compraventa realizada dentro de Yoon u Auto finalice correctamente y el
contrato sea validado, el sistema ofrecerá al usuario que adquiere el vehículo:
Ajouter ce véhicule à Mon Garage
Gran parte de la información ya estará disponible y no deberá volver a introducirse:
## • Marca.
## • Modelo.
## • Versión.
## • Año.
## • Combustible.
## • Cambio.
- VIN, si figura en el contrato/anuncio.
- Matrícula, si está disponible.
- Kilometraje en el momento de la compra.
- Fotografías del anuncio.
- Fecha de adquisición.
- Precio de compra.
El usuario podrá revisar y completar los datos antes de incorporarlo.
B. Vehículo adquirido fuera de Yoon u Auto
Desde Mon Garage existirá: + Ajouter un véhicule
Se solicitará:
Datos principales
## • Marca.
## • Modelo.
## • Versión.
## • Año.
- Kilometraje actual.
## • Combustible.
## • Cambio.
## • Carrocería.
- Potencia, opcional.
- Cilindrada, opcional.
## • Color.
## Identificación

- Matrícula, opcional.
- VIN, opcional.
## Adquisición
- Fecha de compra, opcional.
- Precio pagado, opcional.
## Fotografías
- Fotografía principal.
- Fotografías adicionales.
No debe ser obligatorio completar todos los campos para crear el vehículo. El usuario
podrá completar posteriormente la ficha.
Tarjeta del vehículo
Cada vehículo aparecerá en Mon Garage mediante una tarjeta:
Ejemplo: Toyota RAV4 2019
147.500 km
Valeur estimée: 8.400.000 FCFA
Prochain rappel: Vidange · 2.500 km
Acciones principales:
Voir · Mettre à jour · Vendre
- Historial documental
Cada vehículo tendrá un espacio privado destinado a almacenar su documentación.
## Documents
El usuario podrá subir y clasificar documentos relacionados con el vehículo.
Por ejemplo:
- Contrato de compraventa.
- Permiso/documentación del vehículo.
- Documentación aduanera.
## • Seguro.
## • Inspecciones.
- Facturas de mantenimiento.
- Facturas de reparaciones.
- Factura de compra.
- Otros documentos.
Cada documento almacenará:
- Tipo de documento.
## • Nombre.
- Fecha del documento.
## • Archivo.
- Observaciones opcionales.
- Fecha de subida.
Los documentos se ordenarán cronológicamente.
## Privacidad
Toda la documentación de Mon Garage será privada por defecto.
Ningún otro usuario podrá acceder a ella.

Cuando el propietario decida vender el vehículo podrá elegir posteriormente qué
información de su historial desea hacer visible.
Los documentos que contengan datos personales nunca deben hacerse públicos
automáticamente.
- Historial de mantenimiento
Cada vehículo tendrá un apartado:
## Entretien
El objetivo es construir progresivamente el historial de mantenimiento del vehículo.
El usuario podrá registrar manualmente actuaciones como:
- Cambio de aceite.
## • Filtros.
## • Neumáticos.
## • Frenos.
## • Batería.
## • Distribución.
## • Embrague.
## • Suspensión.
- Aire acondicionado.
- Reparación de motor.
- Revisión general.
## • Otro.
Cada mantenimiento podrá contener
- Tipo de intervención.
## • Fecha.
## • Kilometraje.
## • Descripción.
- Coste, opcional.
- Taller, opcional.
- Factura/documento, opcional.
- Fotografías, opcionales.
## • Observaciones.
## Ejemplo:
Vidange + filtre à huile
12/06/2026 · 145.320 km
## 35.000 FCFA
Facture disponible ✓
El usuario podrá editar una entrada si ha cometido un error.
El sistema deberá conservar la fecha de creación/modificación para mantener
trazabilidad.
## Historial
Todas las intervenciones aparecerán cronológicamente:

## 2026
→ Vidange · 145.320 km
→ Pneus avant · 138.400 km
## 2025
→ Batterie · 126.000 km
→ Révision générale · 118.500 km
Con el paso del tiempo se construirá así un historial real del vehículo.
## 4. Recordatorios
El usuario podrá crear recordatorios relacionados con cada vehículo.
## Rappels
Podrán estar asociados a:
## Fecha
Por ejemplo:
## Assurance
20 novembre 2026
## Kilometraje
Por ejemplo:
Prochaine vidange
À 150.000 km
Fecha + kilometraje
Por ejemplo:
## Vidange
15 décembre 2026 ou 150.000 km
El usuario podrá crear recordatorios para:
- Cambio de aceite.
## • Seguro.
## • Inspección.
## • Neumáticos.
## • Distribución.
## • Frenos.
## • Revisión.
## • Otros.
## Estados:
- À venir
- À faire
## • Terminé
## • Annulé
Cuando corresponda, el usuario recibirá una notificación.
Para recordatorios basados en kilómetros, será necesario que el usuario actualice
periódicamente el kilometraje del vehículo.
No se debe inventar automáticamente el kilometraje recorrido.
- Valor estimado
Esta es una de las funcionalidades con mayor potencial de fidelización.

Cada vehículo podrá mostrar:
Valeur estimée
## 8.200.000 – 8.600.000 FCFA
La estimación se calculará sin Inteligencia Artificial.
Se utilizarán datos estadísticos disponibles en Yoon u Auto de vehículos comparables.
Variables principales:
## • Marca.
## • Modelo.
- Versión, cuando haya suficiente información.
## • Año.
## • Kilometraje.
## • Combustible.
## • Cambio.
- Estado aduanero, cuando proceda.
## • Ubicación.
- Precios de vehículos similares.
- Datos históricos disponibles.
El sistema debe establecer una muestra mínima antes de generar una estimación.
Si no existen suficientes vehículos comparables:
Pas assez de données pour estimer la valeur de ce véhicule.
Nunca debe inventarse una valoración.
## Importante
La valoración es orientativa.
Debe indicarse:
Estimation basée sur les données disponibles sur Yoon u Auto. Il ne s'agit pas
d'une offre d'achat.
Esto es importante porque “cuánto vale mi coche” y “cuánto me ofrece alguien por
mi coche” son conceptos diferentes.
En la V1 estamos ofreciendo una estimación, no comprometiéndonos a comprarlo.
- Evolución del valor
Cuando existan suficientes datos, el usuario podrá consultar cómo ha evolucionado la
estimación de su vehículo.
Évolution de la valeur
## Ejemplo:
## Fecha
- Valor estimado
## Enero 2026
## • 9.100.000 FCFA
## Abril 2026
## • 8.850.000 FCFA
## Agosto 2026
## • 8.400.000 FCFA
Puede representarse mediante un gráfico sencillo.
El objetivo es que el usuario pueda comprender:

Valor actual aproximado.
Evolución durante los últimos meses.
Diferencia respecto al valor anterior.
Tendencia general.
## Ejemplo:
Valeur actuelle estimée: 8.400.000 FCFA
6 derniers mois: ↓ 450.000 FCFA (-5,1 %)
No utilizar IA ni realizar predicciones futuras en el MVP.
- Salud del vehículo
Aquí haría una precisión importante respecto a lo que habíamos definido inicialmente.
Santé du véhicule no debe pretender decir si mecánicamente el coche está bien o
mal.
Yoon u Auto no dispone de información suficiente para hacerlo.
Será un indicador de calidad y actualización del historial digital del vehículo.
Podemos incluso llamarlo:
Complétude du véhicule
o mantener comercialmente Santé du véhicule, pero explicando claramente qué
significa.
## Ejemplo:
Santé de votre dossier: 82 %
La puntuación podrá aumentar cuando:
- Datos principales completos.
- Kilometraje actualizado.
- VIN registrado.
- Fotografías actualizadas.
- Documentación añadida.
- Historial de mantenimiento registrado.
- Recordatorios atendidos.
- Facturas asociadas a mantenimientos.
## Ejemplo:
82 % — Très bien
✓ Informations principales
✓ Kilométrage actualisé
✓ 4 entretiens enregistrés
⚠ Assurance à ajouter
⚠ Photo principale ancienne
No utiliza IA.
Será simplemente una puntuación calculada mediante reglas.
Regla fundamental
Nunca deberá presentarse como diagnóstico mecánico ni certificación del estado
del vehículo.
Esto debe quedar explícito para el desarrollador.
- Preparar futura venta

Esta funcionalidad conecta la Etapa 3 nuevamente con la Etapa 1 y cierra el ciclo de
Yoon u Auto.
En cualquier momento, desde un vehículo de Mon Garage, el propietario podrá pulsar:
Vendre ce véhicule
El sistema creará automáticamente un borrador de anuncio utilizando la información
que ya existe en Mon Garage.
Podrá precargar:
## • Marca.
## • Modelo.
## • Versión.
## • Año.
## • Combustible.
## • Cambio.
## • Carrocería.
## • Potencia.
## • Cilindrada.
## • Color.
- VIN, aunque no necesariamente será público.
- Fotografías existentes.
- Equipamiento registrado.
## • Kilometraje.
El usuario deberá revisar especialmente:
- Kilometraje actual.
- Precio de venta.
## • Fotografías.
- Estado actual.
## • Descripción.
## • Equipamiento.
- Estado aduanero.
## • Ubicación.
Y posteriormente:
Publier l'annonce
No se publicará nada automáticamente.
Compartir historial al vender
Aquí recuperaría la idea que definimos anteriormente porque puede ser muy potente.
Cuando el usuario decide vender el vehículo podrá elegir qué parte de su historial
quiere mostrar en el anuncio.
Transparence du véhicule
Por ejemplo:
☑ Mostrar historial de mantenimiento.
☑ Mostrar fechas y kilometraje de mantenimientos.
☑ Mostrar facturas seleccionadas.

☑ Mostrar evolución registrada del kilometraje.
☑ Mostrar determinadas reparaciones.
Los documentos privados no se publicarán automáticamente.
El usuario deberá seleccionar expresamente aquello que desea compartir.
Esto permitiría que algunos anuncios tengan, por ejemplo:
Historique d'entretien disponible ✓
o:
7 entretiens enregistrés sur Yoon u Auto
Eso puede convertirse con el tiempo en un elemento muy potente de confianza.
Pantalla principal de Mon Garage
Con todas estas funcionalidades, evitaría que Mon Garage fuera simplemente una lista.
Arriba podría mostrar un pequeño resumen:
## Mon Garage
2 véhicules
Valeur estimée totale: 14.700.000 FCFA
1 rappel à venir
Después aparecen las tarjetas.
Toyota RAV4
2019 · 147.500 km
Valeur estimée
## 8.400.000 FCFA
## Entretien
Dernière vidange: 145.320 km
Prochain rappel
Vidange dans 2.500 km
## Dossier
## 82 %
Voir le véhicule · Vendre
Relación con el resto de Yoon u Auto
Esta etapa cierra el círculo completo:
- Encuentra tu coche
Marketplace · Mes recherches · Favoris · Comparateur · Trouvez-moi cette voiture
## ↓
- Negocia y compra
Chat · Oferta · Inspección · Acuerdo · Contrato · Venta verificada
## ↓
- Sigue usando Yoon u Auto
## Mon Garage · Documentos · Mantenimiento · Recordatorios · Valor
## ↓
Vendre ce véhicule
## ↓
El coche vuelve al Marketplace.
Y el usuario puede volver a Mes recherches para encontrar el siguiente.

Ese ciclo me parece bastante más potente que pensar Yoon u Auto únicamente como
una web de anuncios. Mi Garaje es precisamente lo que permite que el usuario
tenga un motivo para conservar la cuenta y volver aunque lleve dos años sin
comprar ni vender nada.


- Menú o i Espacio
La navegación de Yoon u Auto debe ser sencilla y orientada a las acciones principales que
realiza el usuario. Una vez registrado, el usuario podrá acceder directamente desde el menú
a sus principales áreas de actividad.
La estructura propuesta sería:
-           Mes recherches
-      Mes négociations
## •           Mon Garage
-         Mes annonces
-       Mon profil
## •        Paramètres
Y la campana       Notifications estará permanentemente accesible desde el encabezado, sin
constituir una sección principal del menú.
Prochainement tampoco estoy seguro de que merezca ser una sección principal. Yo lo
pondría dentro de Mon profil o como banners/contextos en la aplicación. No debemos llenar
el menú con algo que todavía no existe.
Mes recherches
Es el centro de toda la actividad relacionada con encontrar un vehículo.
Contendrá las cuatro secciones que ya hemos desarrollado:
Favoris · Recherches enregistrées · Comparateur · Mes demandes
Por tanto, aquí se concentra:
- Vehículos favoritos.
- Seguimiento de bajadas de precio.
- Búsquedas guardadas.

- Alertas de nuevos vehículos.
- Comparador de hasta tres vehículos.
- Solicitudes Trouvez-moi cette voiture.
- Seguimiento de esas solicitudes de importación.
Es básicamente la prolongación personal de la Etapa 1 — Encuentra tu coche.

Mes négociations
Centraliza todo lo que sucede desde que un usuario demuestra interés real por un vehículo
hasta que la compraventa termina.
No tendremos módulos independientes llamados Mes messages, Mes offres y Mes contrats.
Todo pertenece a una negociación.
Cada negociación estará vinculada a:
Anuncio + usuario interesado + usuario que publica
Dentro estarán:
## • Chat.
- Fotografías/vídeos intercambiados.
- Solicitudes de información adicional.
## • Ofertas.
## • Contraofertas.
- Oferta aceptada/rechazada.
- Checklist privada de inspección.
- Acuerdo alcanzado.
- Creación del contrato.
- Revisión del contrato.
- Solicitud de modificación.
## • Validación.

## • PDF.
- QR de verificación.
## • Timeline.
- Venta verificada.
Podrá organizarse mediante:
En cours · En attente · Terminées
Es la prolongación natural de la Etapa 2 — Negocia y compra.

## Mon Garage
Aquí se gestionan los vehículos que pertenecen al usuario, hayan sido comprados o no
mediante Yoon u Auto.
## Contendrá:
- Añadir vehículo.
- Datos del vehículo.
## • Kilometraje.
## • Fotografías.
## • Documentación.
- Historial de mantenimiento.
## • Facturas.
## • Recordatorios.
- Valor estimado.
- Evolución del valor.
- Estado/completitud del expediente.
- Preparación para futura venta.
Y la acción fundamental:
Vendre ce véhicule

permitirá convertir un vehículo del Garaje en un borrador de anuncio.
Es el núcleo de la Etapa 3 — Sigue usando Yoon u Auto.

Mes annonces
Aquí estarán exclusivamente los vehículos que el usuario está vendiendo o ha vendido.
## Permitirá:
- Crear anuncio.
## • Editar.
## • Completar.
- Añadir/reordenar fotografías.
- Actualizar precio.
- Actualizar kilometraje.
## • Pausar.
## • Reactivar.
- Marcar como reservado.
- Marcar como vendido.
## • Duplicar.
## • Compartir.
## • Archivar.
- Consultar estadísticas básicas.
- Consultar calidad/completitud del anuncio.
## Estados:
Brouillon · Actif · En pause · Réservé · Vendu · Archivé
Además, desde un anuncio podrá accederse directamente a sus negociaciones asociadas.
Así mantenemos clara la diferencia:

Mon Garage = coches que poseo.
Mes annonces = coches que estoy ofreciendo en el Marketplace.
Un mismo vehículo puede estar relacionado con ambos módulos.

Mon profil
Este apartado contiene exclusivamente la identidad del usuario.
## • Nombre.
## • Foto/avatar.
## • Teléfono.
- Teléfono verificado.
## • Ciudad.
## • Particulier / Professionnel.
- Fecha de alta.
- Ventas verificadas.
- Número de anuncios activos.
Y las preferencias relacionadas con cómo otros usuarios pueden contactar con él.
Por ejemplo:
Autoriser le contact par WhatsApp: ON/OFF
La mensajería interna seguirá siendo el canal principal.
Y mantenemos nuestra regla:
Particulier / Professionnel es únicamente un campo del perfil en la V1. No genera
interfaces, permisos ni funcionalidades diferentes.

## Paramètres
Yo lo dejaría pequeño:
## • Notificaciones.
## • E-mail.

## • Privacidad.
## • Idioma.
## • Seguridad.
- Gestión de cuenta.
- Cerrar sesión.
- Eliminar cuenta.
Las preferencias específicas —por ejemplo, recibir alertas cuando baja un Favorito— no
van aquí. Se gestionan dentro del propio módulo al que pertenecen.

## Notifications
No aparece como opción principal del menú.
La campana permanece visible en el header y reúne eventos de toda la aplicación:
Nouveau message · Nouvelle offre · Contre-offre · Contrat à valider · Baisse de prix ·
Nouvelle voiture correspondant à votre recherche · Véhicule trouvé · Rappel
d'entretien, etc.

Incluso simplificaría un poco más
Tenemos que distinguir entre navegación pública y navegación personal.
Un visitante necesita principalmente:
Acheter / Voitures · Vendre / Publier · Trouvez-moi une voiture
Mientras que cuando el usuario está autenticado aparecen sus grandes espacios:
Mes recherches · Mes négociations · Mon Garage · Mes annonces
Y luego Perfil/Configuración puede estar bajo el avatar       en vez de ocupar dos huecos
del menú principal.
Por tanto, la navegación personal realmente importante se reduce a cuatro conceptos:
Mes recherches → lo que quiero comprar.
Mes négociations → lo que estoy negociando.

Mon Garage → lo que ya tengo.
Mes annonces → lo que estoy vendiendo.
Creo que esta es la estructura más limpia que hemos tenido hasta ahora. Además, las
cuatro secciones representan casi perfectamente el ciclo real del usuario en Yoon u Auto.

## 6. Administración

Yoon u Auto deberá disponer de un área de administración privada, accesible
exclusivamente para usuarios autorizados con rol de administrador.
El backoffice permitirá gestionar y supervisar la actividad de la plataforma sin necesidad de
acceder directamente a la base de datos.
Desde Administración se podrán gestionar:
## • Usuarios.
## • Anuncios.
- Negociaciones y actividad.
- Solicitudes Trouvez-moi cette voiture.
- Contratos y ventas verificadas.
- Moderación y reportes.
## • Notificaciones/comunicaciones.
- Estadísticas generales.
- Datos utilizados para indicadores de precio.
- Configuración básica del sistema.
El panel de administración debe estar completamente separado de la experiencia
normal del usuario.
6.2. Tableau de bord — Dashboard general
La pantalla inicial mostrará una visión resumida del estado de Yoon u Auto.
Indicadores principales
## Usuarios
- Usuarios totales.
- Nuevos usuarios hoy.
- Nuevos usuarios últimos 7/30 días.
## • Particulares.
## • Profesionales.
- Usuarios con teléfono verificado.
## Marketplace
- Anuncios activos.
- Nuevos anuncios.
- Vehículos reservados.
- Vehículos vendidos.
## • Borradores.
- Anuncios pausados.

- Anuncios pendientes de revisión/moderación.
## Actividad
- Negociaciones iniciadas.
- Negociaciones activas.
- Mensajes enviados.
- Ofertas realizadas.
- Ofertas aceptadas.
- Contratos creados.
- Contratos validados.
- Ventas verificadas.
## Demanda
- Búsquedas guardadas.
- Vehículos añadidos a Favoritos.
- Modelos más buscados.
- Modelos más guardados.
- Solicitudes Trouvez-moi cette voiture pendientes.
- Solicitudes en búsqueda.
- Vehículos solicitados con mayor frecuencia.
## Mon Garage
- Vehículos incorporados a garajes.
- Vehículos procedentes de compras en Yoon u Auto.
- Vehículos externos añadidos manualmente.
- Vehículos convertidos posteriormente en anuncios.
El objetivo no es crear inicialmente un sistema complejo de Business Intelligence, sino
permitir al administrador conocer rápidamente qué está ocurriendo en la
plataforma.
6.3. Gestion des utilisateurs
El administrador podrá buscar, consultar y gestionar usuarios.
## Listado
Permitirá buscar/filtrar por:
## • Nombre.
## • Teléfono.
## • Ciudad.
## • Particular / Professionnel.
- Teléfono verificado.
- Fecha de registro.
- Estado de cuenta.
- Número de anuncios.
- Número de ventas verificadas.
Ficha del usuario
Al acceder a un usuario se podrá consultar:
Datos del perfil
## • ID.

## • Nombre.
## • Teléfono.
## • Verificación.
## • Ciudad.
## • Particular / Professionnel.
- Fecha de registro.
- Última actividad.
## • Estado.
## Actividad
- Anuncios publicados.
- Anuncios vendidos.
## • Negociaciones.
## • Ofertas.
## • Contratos.
- Ventas verificadas.
- Solicitudes Trouvez-moi cette voiture.
- Vehículos en Mon Garage, respetando las restricciones de privacidad que definamos.
Acciones administrativas
- Activar/desactivar cuenta.
- Suspender temporalmente.
## • Bloquear.
- Consultar reportes recibidos.
- Añadir notas internas.
- Consultar histórico de acciones administrativas.
El administrador no debe poder modificar libremente información sensible del
usuario sin dejar trazabilidad.
6.4. Gestion des annonces
Será uno de los módulos administrativos principales.
Listado de anuncios
## Filtros:
## • Referencia Yoon.
## • Marca.
## • Modelo.
## • Usuario.
## • Ciudad.
## • Fecha.
## • Precio.
## • Estado.
- Estado aduanero.
## • Particular / Professionnel.
- Reportado/no reportado.
## Estados:
Brouillon · Actif · En pause · Réservé · Vendu · Archivé

Ficha administrativa del anuncio
Debe mostrar toda la información pública del anuncio y además información
administrativa:
- ID interno.
## • Referencia Yoon.
- Usuario propietario.
- Fecha de creación.
- Última modificación.
- Historial de precios.
## • Estado.
- Número de visualizaciones.
## • Favoritos.
- Negociaciones iniciadas.
- Ofertas recibidas.
## • Reportes.
- Calidad/completitud del anuncio.
## Acciones
El administrador podrá:
## • Consultar.
- Ocultar temporalmente.
## • Reactivar.
- Marcar para revisión.
## • Archivar.
- Eliminar cuando corresponda.
- Consultar motivo de reportes.
- Contactar con el usuario.
- Añadir notas internas.
No permitiría al administrador modificar alegremente marca, kilómetros, precio, etc. La
información comercial pertenece al usuario que publica.
Si hay información incorrecta, lo normal será solicitar su corrección.
## 6.5. Modération
La moderación merece un módulo propio.
Los usuarios podrán reportar:
- Anuncio sospechoso.
- Información falsa.
- Precio engañoso.
- Fotografías incorrectas.
- Vehículo inexistente.
- Intento de fraude.
- Comportamiento inapropiado.
## • Spam.
- Otro motivo.
El administrador recibirá:

Signalement #XXXX
con:
- Usuario que reporta.
- Usuario reportado.
- Anuncio/negociación relacionada.
## • Motivo.
## • Descripción.
## • Fecha.
- Evidencias, si existen.
## Estados
Nouveau · En examen · Résolu · Rejeté
## Acciones
Según el caso:
- No actuar.
## • Advertir.
- Ocultar anuncio.
- Suspender usuario.
- Bloquear usuario.
- Solicitar información.
- Cerrar reporte.
Todas las acciones importantes deberán registrarse.
6.6. Gestion de « Trouvez-moi cette voiture »
Este módulo es especialmente importante porque aquí el administrador deja de ser
únicamente moderador y empieza a prestar un servicio al usuario.
Todas las solicitudes creadas desde:
Mes recherches → Mes demandes
llegarán a:
Administration → Demandes de véhicules
## Listado
## Mostrar:
## • Referencia.
## • Usuario.
## • Marca/modelo.
## • Año.
- Kilometraje máximo.
## • Presupuesto.
## • Procedencia.
## • Fecha.
## • Estado.
- Administrador responsable, si posteriormente tenemos varios.
## Estados:
Nouvelle demande → En recherche → Véhicule proposé → Terminée / Annulée
Dentro de la solicitud

El administrador podrá:
- Consultar todos los criterios.
- Consultar al usuario.
- Añadir notas internas.
- Cambiar estado.
- Comunicarse con el usuario.
- Buscar/anexar anuncios existentes de Yoon u Auto.
- Añadir propuestas externas.
- Consultar el histórico de la solicitud.
## • Finalizarla.
Propuesta externa
Si se encuentra un vehículo fuera de Yoon u Auto, podrá introducir:
## • Marca/modelo/versión.
## • Año.
## • Kilometraje.
## • Combustible.
## • Cambio.
## • País.
## • Precio.
- Costes adicionales conocidos, si procede.
## • Fotografías.
- Enlace de origen, opcional.
## • Comentarios.
El usuario recibirá:
Nous avons trouvé un véhicule pour vous.
Y podrá consultar la propuesta desde su solicitud.
6.7. Gestion des négociations
Aquí tendría cuidado.
El administrador no debería participar normalmente en negociaciones privadas
entre usuarios, pero sí necesitamos herramientas administrativas.
El panel podrá mostrar datos estructurales como:
- Negociaciones activas.
- Anuncio relacionado.
- Usuarios participantes.
- Fecha de inicio.
- Última actividad.
- Existencia de ofertas.
- Estado general.
- Contrato asociado.
- Reportes asociados.
## Privacidad
No daría por hecho que el administrador pueda sentarse a leer indiscriminadamente
todas las conversaciones privadas.

El acceso al contenido deberá limitarse a situaciones justificadas como:
## • Reporte.
## • Moderación.
## • Disputa.
- Investigación de fraude.
- Soporte solicitado.
Y ese acceso debería quedar registrado.
Esto conviene definir desde el principio.
6.8. Gestion des contrats et ventes
Permitirá controlar el flujo contractual sin alterar arbitrariamente contratos entre
usuarios.
## Listado
## • Referencia.
## • Anuncio.
## • Usuarios.
- Precio acordado.
## • Fecha.
## • Estado.
Estados posibles:
Brouillon → Envoyé → À valider → Modification demandée → Validé → Annulé
## Ficha
- Datos de ambas partes.
## • Vehículo.
## • Precio.
## • Timeline.
- Fecha de creación.
- Fecha de envío.
## • Validaciones.
## • PDF.
## • QR.
- Venta verificada asociada.
## Administrador
## Podrá:
- Consultar estado.
- Gestionar incidencias.
- Invalidar administrativamente un contrato en situaciones excepcionales.
- Consultar PDF.
- Verificar QR.
- Consultar trazabilidad.
Pero una regla importante:
El administrador no puede validar un contrato en nombre del comprador o
vendedor.
La validación debe pertenecer a las partes.

6.9. Ventas verificadas y fidelización
Cuando el proceso contractual finaliza correctamente:
Vente vérifiée ✓
El administrador podrá consultar:
## • Venta.
## • Vehículo.
- Usuario que vende.
- Usuario que compra.
## • Contrato.
## • Fecha.
- Puntos generados.
Esto alimentará automáticamente:
- Número de ventas verificadas del usuario.
- Sistema de fidelización.
## Puntos
En el MVP yo permitiría al administrador consultar:
## • Saldo.
## • Origen.
## • Fecha.
## • Movimiento.
## Ejemplo:
Vente vérifiée #YV00125
+100 points
Y si hay ajustes manuales:
Ajustement administrateur
+50 points
Motif: geste commercial
Cualquier ajuste manual debe registrar administrador, fecha y motivo.
6.10. Gestion du Marketplace y datos de referencia
Hay determinados datos que no deberían estar escritos directamente en el código.
El administrador debe poder mantener catálogos como:
## • Marcas.
## • Modelos.
## • Carrocerías.
## • Combustibles.
## • Cambios.
## • Equipamientos.
## • Regiones.
## • Ciudades.
## • Colores.
- Estados aduaneros, si fuera necesario.
Por ejemplo:
Toyota → Corolla / Hilux / RAV4 / Prado...

Esto permite ampliar el catálogo sin tener que pedir al desarrollador que modifique
código.
6.11. Indicador estadístico de precio
También necesitamos administración para una funcionalidad que ya hemos definido:
Bonne affaire · Prix correct · Prix élevé
El cálculo es automático y estadístico, pero sus parámetros deben poder configurarse.
El administrador podrá consultar:
- Número de vehículos comparables.
- Precio medio/mediano de referencia.
- Rangos utilizados.
- Resultado asignado.
Y configurar parámetros generales como:
- Número mínimo de anuncios comparables.
- Antigüedad máxima de los anuncios utilizados.
- Margen porcentual para Bonne affaire.
- Margen para Prix correct.
- Margen para Prix élevé.
Así no tenemos esos porcentajes enterrados en el código.
No se utiliza IA.
6.12. Notifications et communications
El administrador debe poder generar determinadas comunicaciones de plataforma.
Por ejemplo:
- Aviso general.
- Mantenimiento programado.
- Información importante.
- Comunicación individual relacionada con soporte.
- Comunicación asociada a una solicitud de vehículo.
Pero evitaría convertir el MVP en una plataforma de marketing compleja.
Canales inicialmente
- Notificación interna.
- E-mail cuando proceda.
El histórico debe registrar qué se envió, cuándo y a quién.
## 6.13. Statistiques
Aquí es donde empezamos a obtener información realmente interesante del negocio.
## Usuarios
- Altas por día/semana/mes.
- Usuarios activos.
- Distribución geográfica.
## • Particular / Professionnel.
## Oferta
- Vehículos publicados.
- Marcas más publicadas.
## • Modelos.

- Precio medio/mediano.
## • Kilometraje.
## • Año.
## • Ciudad.
## • Combustible.
- Estado aduanero.
## Demanda
Esta me parece más importante todavía:
- Marcas más buscadas.
- Modelos más buscados.
- Filtros más utilizados.
- Búsquedas guardadas.
- Modelos más añadidos a Favoritos.
- Vehículos más comparados.
- Presupuestos de búsqueda.
- Solicitudes Trouvez-moi cette voiture.
- Modelos pedidos que no existen suficientemente en Marketplace.
Esto último puede ser oro para el futuro negocio con importadores y concesionarios.
Por ejemplo:
## Toyota Hilux
184 usuarios buscando
37 solicitudes
11 anuncios disponibles
Yoon empieza a conocer el gap entre oferta y demanda.
## Conversión
## • Visualización → Favorito.
- Visualización → conversación.
- Conversación → oferta.
- Oferta → acuerdo.
- Acuerdo → contrato.
- Contrato → venta verificada.
Esto permite detectar dónde se pierde al usuario.
6.14. Interés en futuras funcionalidades
Aquí conectamos con Prochainement.
Cuando un usuario pulsa:
Ça m'intéresse
el administrador podrá consultar:
## • Funcionalidad
- Usuarios interesados
- Gestion de stock
## • 148
- WhatsApp Business
## • 103

## • Funcionalidad
- Usuarios interesados
## • CRM
## • 87
- Tendances du marché
## • 216
- Outils intelligents
## • 165
- Y podrá segmentarlo por:
## • Particular / Professionnel.
## • Ciudad.
## • Actividad.
- Número de anuncios.
Esto nos permite decidir qué servicio premium merece realmente desarrollarse.
6.15. Configuration générale
Finalmente tendremos algunos parámetros básicos administrables.
Por ejemplo:
- Número máximo de vehículos en Comparateur: inicialmente 3.
- Número mínimo de comparables para indicador de precio.
- Parámetros del sistema de puntos.
## • Catálogos.
- Estados disponibles.
- Configuración de notificaciones.
- Textos legales/versiones.
- Funcionalidades activadas/desactivadas mediante feature flags, cuando proceda.
La idea es evitar que cualquier pequeño cambio de negocio requiera modificar el código.
6.16. Registro de actividad administrativa
Añadiría esto aunque el usuario nunca lo vea.
Las operaciones sensibles del administrador deben generar un registro:
Audit log
Por ejemplo:
## 09/08/2026 14:32
## Admin #02
A masqué l'annonce #YA-00824
Motif: signalement en cours
o:
## Admin #01
+50 points utilisateur #U239
Motif: geste commercial
## Registrar:
## • Administrador.
## • Acción.
- Entidad afectada.
## • Fecha/hora.

- Valor anterior/nuevo cuando proceda.
## • Motivo.
Esto será muy útil cuando haya varios administradores.
Estructura del Backoffice
Yo dejaría finalmente el menú administrativo aproximadamente así:
-         Tableau de bord
## •         Utilisateurs
## •           Annonces
-           Demandes de véhicules
## •      Négociations
-       Contrats & ventes
## •       Modération
## •       Communications
## •       Statistiques
## •        Configuration
Y una cuestión de arquitectura importante: Mon Garage no necesita un módulo
administrativo propio en el MVP. Es fundamentalmente un espacio privado. El
administrador solo debería acceder a datos concretos cuando exista una razón de
soporte, moderación o gestión, no porque pueda navegar libremente por los garajes
privados de todos los usuarios.
Con esto, Administración ya deja de ser simplemente “gestionar usuarios y anuncios”. Se
convierte en el centro operativo de Yoon u Auto, incluyendo especialmente dos
activos que pueden ser estratégicos para el negocio futuro: entender qué coches se
están demandando y gestionar directamente las solicitudes de importación de
“Trouvez-moi cette voiture”.
