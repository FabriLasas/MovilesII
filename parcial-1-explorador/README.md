# Explorador de Personajes

[![Explorador de Personajes](https://github.com/FabriLasas/MovilesII/actions/workflows/explorador.yml/badge.svg)](https://github.com/FabriLasas/MovilesII/actions/workflows/explorador.yml)

Aplicación **.NET MAUI** desarrollada para los exámenes parciales de
*Desarrollo de Aplicaciones Móviles 2*. Versión actual: **2.0** (Parcial 2).

Consume la API pública de [Rick and Morty](https://rickandmortyapi.com/) y
permite listar, buscar y filtrar personajes, consultar el detalle de cada uno y
guardar favoritos en una base SQLite. Desde el detalle se registran
**avistamientos** con una foto (cámara o galería), una nota y la ubicación GPS,
y agitando el teléfono se abre un personaje al azar. Sigue funcionando sin
conexión con los datos guardados.

> La versión entregada en el **Parcial 1** quedó congelada en la etiqueta
> [`parcial-1`](https://github.com/FabriLasas/MovilesII/tree/parcial-1/parcial-1-explorador).
> Esta carpeta contiene la evolución del proyecto para el **Parcial 2**.

## Evolución desde el Parcial 1

| Área | Parcial 1 | Parcial 2 |
|---|---|---|
| Arquitectura | Un solo proyecto MAUI | Solución en capas: núcleo `ExploradorPersonajes.Core` sin MAUI, app MAUI y proyecto de pruebas |
| Dependencias del dispositivo | Interfaces de MAUI usadas en los ViewModels (`IHapticFeedback`, `IQueryAttributable`) | Interfaces propias del núcleo; las implementaciones de MAUI viven en `Plataforma/` |
| Favoritos | Ids como texto en `Preferences`; sin red dependían de la caché | Tabla SQLite con los datos y el orden; funcionan sin red. Migración automática de los favoritos viejos |
| Pruebas | Ninguna | 176 pruebas: unitarias, con mocks y de integración con SQLite en memoria |
| Integración continua | Ninguna | GitHub Actions: pruebas, matriz Android y Windows, caché y artefactos |
| Hardware | Vibración corta al marcar favoritos | Cámara, galería, GPS, acelerómetro y vibración, con permisos en tiempo de ejecución |
| Validaciones | Ninguna | Reglas centralizadas en `Validaciones/`, usadas por el formulario y la búsqueda |
| Excepciones | Errores HTTP y de red clasificados | Además, un último recurso para fallas no previstas y protección de las tareas que corren sin que nadie las espere |

---

## Cómo ejecutar

Requiere el **SDK de .NET 10** con las cargas de trabajo de MAUI instaladas.

```bash
dotnet restore ExploradorPersonajes.slnx
```

En Windows:

```bash
dotnet build src/ExploradorPersonajes -f net10.0-windows10.0.19041.0
```

En Android (con un emulador o dispositivo conectado):

```bash
dotnet build src/ExploradorPersonajes -t:Run -f net10.0-android
```

Pruebas automatizadas (no necesitan emulador ni las cargas de trabajo de MAUI):

```bash
dotnet test tests/ExploradorPersonajes.Tests
```

---

## Arquitectura

El proyecto aplica el patrón **MVVM** y está dividido en capas, cada una en su
propio proyecto. Las dependencias apuntan siempre hacia adentro: la app conoce
al núcleo, y el núcleo no conoce ni a la app ni a MAUI.

```
ExploradorPersonajes.slnx
└── src/
    ├── ExploradorPersonajes.Core/   Biblioteca .NET común, sin MAUI
    │   ├── Modelos/                 Objetos de datos y mapeo del JSON
    │   ├── Datos/                   Base SQLite: tablas y repositorios de favoritos y avistamientos
    │   ├── Validaciones/            Reglas comunes y validadores de cada formulario
    │   ├── Servicios/               API, caché, favoritos e interfaces de lo que depende del dispositivo
    │   ├── ViewModels/              Estado y comandos de cada pantalla y de cada tarjeta
    │   └── Mensajes/                Avisos entre pantallas (favorito agregado o quitado)
    └── ExploradorPersonajes/        Aplicación .NET MAUI
        ├── Vistas/                  XAML, sin lógica en el code-behind
        │   └── Plantillas/          Tarjeta de personaje compartida entre pantallas
        ├── Plataforma/              Implementaciones con las APIs del dispositivo
        ├── Comportamientos/         Animaciones reutilizables declaradas desde XAML
        ├── AppShell.xaml            Pestañas y rutas de navegación
        └── MauiProgram.cs           Registro de dependencias: une interfaces e implementaciones
```

Todo lo que depende del dispositivo (red, vibración, navegación con Shell,
la carpeta de datos) está detrás de una interfaz definida en el
núcleo. Gracias a eso el núcleo se puede probar con xUnit en cualquier sistema
operativo, sin emulador.

### Modelos

| Archivo | Rol |
|---|---|
| `Personaje.cs` | Personaje individual, con propiedades calculadas para la vista |
| `Ubicacion.cs` | Objeto anidado que la API usa para origen y ubicación actual |
| `RespuestaPaginada.cs` | Contenedor con los metadatos de paginación |
| `OpcionFiltro.cs` | Opción del filtro por estado |

La API no devuelve una lista plana: envuelve los resultados en un objeto con
metadatos. Por eso hacen falta dos modelos, uno contenedor y uno por elemento.

### ViewModels

| Archivo | Rol |
|---|---|
| `PersonajesViewModel` | Lista: carga, búsqueda, filtro y paginación |
| `FavoritosViewModel` | Pestaña de favoritos |
| `DetalleViewModel` | Detalle, recibe el personaje por navegación |
| `PersonajeItemViewModel` | Una tarjeta: estado de favorito y sus acciones |

`PersonajeItemViewModel` existe porque "es favorito" es estado de la interfaz,
no un dato de la API. Guardarlo dentro de `Personaje` mezclaría el modelo con
la presentación: el modelo queda como datos puros y el ViewModel del ítem lo
envuelve.

### Servicios

| Servicio | Responsabilidad |
|---|---|
| `ServicioPersonajes` | Único punto que habla con la red; clasifica los errores |
| `ServicioCache` | Guarda en disco las últimas respuestas exitosas |
| `ServicioFavoritos` | Favoritos guardados en SQLite, con una copia en memoria para consultas instantáneas |
| `ServicioConectividad` | Estado de la red, con modo sin conexión simulado |
| `ServicioNavegacion` | Encapsula Shell para que los ViewModels no lo conozcan |

La decisión de diseño central es **no usar `EnsureSuccessStatusCode()`**. Ese
método convierte cualquier respuesta no exitosa en una `HttpRequestException`,
que es la misma excepción que se lanza cuando no hay conexión. Al unificarlas se
pierde la información necesaria para dar mensajes distintos. En su lugar se
inspecciona el `StatusCode` y se capturan por separado las excepciones de
transporte.

Los fallos no se propagan como excepciones sino como valor de retorno, dentro de
`ResultadoApi<T>`. Así el ViewModel queda lineal, sin bloques `try/catch`, y
nunca necesita interpretar un código HTTP.

### Manejo de errores

| Situación | Mensaje al usuario |
|---|---|
| Sin acceso a internet | No hay conexión a internet |
| El servidor no responde a tiempo | El servidor tardó demasiado en responder |
| Servidor inalcanzable | No se pudo contactar al servidor |
| Búsqueda sin coincidencias (404) | No se encontraron personajes para esa búsqueda |
| Recurso inexistente (404) | El personaje solicitado no existe |
| Error 5xx | El servidor tuvo un problema interno |
| JSON inesperado | No se pudieron interpretar los datos recibidos |
| Cualquier otra falla no prevista | Ocurrió un error inesperado. Intentá de nuevo |

Las dos filas con 404 merecen atención: la API responde con ese mismo código
tanto cuando una búsqueda no arroja resultados como cuando se pide un personaje
que no existe. En el primer caso no es un error sino un resultado vacío, y se
informa sin formato de alerta. El servicio distingue ambos casos según la
operación que se haya solicitado.

El último caso es el `catch (Exception)` general que recomienda la cátedra.
Va después de todos los específicos (los `catch` se evalúan en orden) y deja
pasar la cancelación pedida por el usuario, que no es un error. Fuera del
servicio, las operaciones con la base local están protegidas en los
ViewModels: si fallan, se informa en pantalla y la app sigue funcionando. Es
especialmente importante en las tareas que arrancan sin que nadie las espere
(por ejemplo, la carga inicial de la pestaña Favoritos), porque una excepción
ahí no llegaría a ningún `catch` y cerraría la aplicación.

#### Sin conexión

Ante un fallo de red (sin conexión, tiempo agotado o servidor inalcanzable) el
servicio busca la última copia guardada de esa misma consulta. Si existe, la
muestra con una franja que avisa la fecha en que se guardó. Si no existe,
informa el error. Un 404 o un 500 nunca se reemplazan por la copia: son
respuestas reales del servidor.

La señal de conectividad del sistema **no se usa para bloquear peticiones**
salvo cuando indica que no hay ninguna red. Durante las pruebas, Windows informó
por un momento que no había internet estando conectado, y la aplicación mostraba
"sin conexión" sin siquiera intentar la llamada. Ahora, en los casos dudosos se
intenta la petición y la señal del sistema solo se usa para explicar un fallo.

#### Demostración

Dos botones de la pantalla principal permiten mostrar el manejo de errores sin
depender de que el servidor falle ni de apagar el wifi:

- **Probar 404** pide el personaje con id 9999, que no existe.
- **Simular sin red** hace que la aplicación se comporte como si no hubiera
  conexión, para ver la copia guardada o el error cuando no la hay.

### Favoritos

Se guardan en una base **SQLite** del dispositivo (`explorador.db3`), con los
datos de cada personaje y el orden en que se marcaron. La pestaña Favoritos
muestra lo guardado al instante, haya red o no, y después pide los datos
actuales a la API para refrescarlos.

| Pieza | Rol |
|---|---|
| `FavoritoEntidad` | Fila de la tabla. Es plana y separada de `Personaje`, que refleja el JSON de la API |
| `IRepositorioFavoritos` / `RepositorioFavoritosSqlite` | Alta, baja, lectura en orden y actualización de datos, con transacciones |
| `ServicioFavoritos` | Lógica de favoritos sobre el repositorio, con una copia en memoria |

**Cambio respecto del Parcial 1.** Antes se guardaban solo los ids en
`Preferences` y los datos se pedían a la API cada vez: sin conexión, la pestaña
dependía de que hubiera una copia en caché de esa consulta exacta. Al abrir la
versión nueva, los ids guardados por la anterior se pasan solos a la base (se
piden sus datos a la API y recién entonces se borran de `Preferences`; si no hay
red, se reintenta en el próximo inicio).

La actualización se pide en **una sola llamada** (`/character/1,5,12`). El
endpoint tiene tres particularidades, verificadas contra la API:

| Comportamiento | Cómo se resuelve |
|---|---|
| Con un solo id devuelve un **objeto**; con varios, un **array** | El caso de un único favorito se pide por separado |
| Devuelve los personajes **ordenados por id**, no en el orden pedido | Se reordenan según el orden en que se marcaron |
| Omite en silencio los ids inexistentes | Se muestran solo los que llegaron |

Un mismo favorito se ve en tres lugares a la vez: la lista, la pestaña y el
detalle. En lugar de que cada ViewModel conozca a los otros, `ServicioFavoritos`
publica un `FavoritoCambiadoMensaje` con el `WeakReferenceMessenger` del Toolkit
y cada pantalla se actualiza por su cuenta. El mensaje lleva el personaje
completo, así que la pestaña lo agrega sin volver a consultar la API, incluso
sin conexión.

### Navegación

Se usa **Shell** con dos pestañas (`TabBar`): Personajes y Favoritos. En Android
se muestran abajo y en Windows arriba. El detalle es una ruta registrada con
`Routing.RegisterRoute` que se apila sobre cualquiera de las dos pestañas.

La navegación pasa por `ServicioNavegacion`, así los ViewModels piden "ir al
detalle de este personaje" sin conocer Shell ni armar diccionarios de
parámetros.

Shell entrega los parámetros llamando a `IQueryAttributable`, una interfaz de
MAUI. Como `DetalleViewModel` vive en el núcleo, recibe el personaje con un
método común (`Recibir`), y la app lo envuelve en `DetalleViewModelShell`, que
agrega la interfaz y solo delega. Se eligió `IQueryAttributable` por sobre el
atributo `[QueryProperty]` porque hace explícito en el código dónde y cómo se
recibe el parámetro, en lugar de depender de un mapeo por reflexión.

### CommunityToolkit.MVVM

Se usa el Toolkit. `ObservableObject` evita implementar `INotifyPropertyChanged`
a mano, y los atributos `[ObservableProperty]` y `[RelayCommand]` generan las
propiedades con notificación y los comandos en tiempo de compilación. El
`WeakReferenceMessenger` comunica las pantallas sin acoplarlas y sin retener en
memoria a las que ya no se usan.

Los comandos asíncronos que genera el Toolkit se deshabilitan mientras se
ejecutan, lo que impide que dos toques seguidos al mismo botón disparen dos
llamadas de red simultáneas. Es el comportamiento por defecto; en el código se
declara de forma explícita con `AllowConcurrentExecutions = false`.

Cada búsqueda lleva un número de versión: si llega la respuesta de una
consulta que ya fue reemplazada por otra (por ejemplo, una página del scroll
infinito que vuelve después de que el usuario escribió algo nuevo), se descarta
en lugar de mezclarse con los resultados actuales.

La contrapartida es que agrega una dependencia externa y que el código generado
no está a la vista, lo que dificulta el seguimiento paso a paso durante la
depuración.

### Validaciones

Las reglas están centralizadas en el núcleo, en `Validaciones/`, y no en las
pantallas: se aplican igual desde donde se use el dato y se prueban sin
interfaz gráfica.

| Pieza | Rol |
|---|---|
| `ReglasComunes` | Reglas reutilizables: dato obligatorio, largo mínimo y máximo, rango de latitud y longitud, id válido. Cada una devuelve el mensaje de error o `null` |
| `ResultadoValidacion` | Junta **todos** los errores, cada uno asociado a su campo, para marcarlos de una sola vez debajo del dato que corresponde |
| `ValidadorAvistamiento` | Reglas de negocio de un avistamiento: foto obligatoria, nota de 3 a 200 caracteres, ubicación opcional pero completa y dentro de rango |

La búsqueda de personajes usa la misma regla de largo máximo: un texto de más de
50 caracteres no se envía a la API (el nombre más largo del catálogo tiene 46) y
se informa el error en lugar de hacer una llamada inútil.

### Avistamientos: cámara, GPS y permisos

Desde el detalle de un personaje se registra un **avistamiento**: una foto, una
nota y, si el usuario quiere, la ubicación. Se guardan en SQLite y se listan en
el mismo detalle, del más reciente al más antiguo.

| Recurso | API de MAUI | Permiso en tiempo de ejecución |
|---|---|---|
| Cámara | `MediaPicker.CapturePhotoAsync` | `Permissions.Camera` |
| Galería | `MediaPicker.PickPhotosAsync` | Ninguno: el selector de fotos del sistema solo entrega la foto elegida |
| GPS | `Geolocation.GetLocationAsync` (precisión media, 10 s) con respaldo en `GetLastKnownLocationAsync` | `Permissions.LocationWhenInUse` |
| Vibración | `Vibration.Vibrate(300 ms)` al guardar; `HapticFeedback` al tocar | No requiere diálogo (`VIBRATE` en el manifiesto) |

Criterios aplicados:

- **Cada permiso se pide en el momento en que hace falta**, nunca todos juntos
  al abrir la app. Antes de mostrar el diálogo se consulta el estado con
  `CheckStatusAsync`: si ya estaba concedido, el usuario no ve nada.
- **Si se niega, la app no falla.** Sin cámara se ofrece la galería; sin
  ubicación, el avistamiento se guarda igual (la ubicación es opcional). En
  ambos casos aparece un botón para abrir los Ajustes de la app, que es la
  única forma de recuperar un permiso negado con "no volver a preguntar".
- **Dispositivos sin el hardware**: el botón de la cámara se oculta si no hay
  cámara, y en el manifiesto la cámara y el GPS se declaran con
  `required="false"` para que la app se pueda instalar igual.
- **Fotos livianas**: se piden a 1280 píxeles y con compresión al 80 %.
- **Sin archivos huérfanos**: la foto queda en una carpeta temporal mientras se
  completa el formulario y recién al guardar se copia a la carpeta de la app.
  Si el usuario cancela o sale con el botón atrás, la temporal se descarta al
  iniciar la app. Al eliminar un avistamiento se borra también su foto, pero
  solo después de quitar el registro de la base.

Configuración por plataforma:

| Archivo | Qué declara |
|---|---|
| `Platforms/Android/AndroidManifest.xml` | `CAMERA`, `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`, `READ_EXTERNAL_STORAGE` (solo hasta Android 12), `VIBRATE`, cámara y GPS como hardware opcional, y la consulta del intent de captura que exige Android 11 |
| `Platforms/iOS/Info.plist` y `Platforms/MacCatalyst/Info.plist` | `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription` y `NSLocationWhenInUseUsageDescription`: el texto que ve el usuario al pedir cada permiso, obligatorio en iOS |

Todo el acceso al hardware está detrás de interfaces del núcleo
(`IServicioPermisos`, `IServicioFotos`, `IServicioUbicacion`, `IServicioHaptica`),
así que el formulario se prueba con mocks: qué pasa si se niega cada permiso, si
el GPS no responde o si la cámara falla.

### Personaje al azar: acelerómetro

En la pantalla principal, **agitar el teléfono** abre el detalle de un
personaje elegido al azar. El botón 🎲 hace lo mismo, para dispositivos sin
acelerómetro (o para quien prefiera tocar).

- Se usa el evento `ShakeDetected` del acelerómetro de MAUI, a velocidad
  `SensorSpeed.Game`, la recomendada para detectar agitados.
- **El sensor solo está encendido mientras la lista está a la vista.**
  `CicloDeVidaBehavior` ejecuta un comando del ViewModel cuando la página
  aparece y otro cuando deja de verse: al abrir un detalle o cambiar de pestaña
  el acelerómetro se apaga y no gasta batería.
- Los avisos del sensor llegan desde otro hilo y se encolan para el principal:
  uno puede llegar cuando la lista ya no está a la vista. El ViewModel solo los
  atiende entre el comando que enciende el sensor y el que lo apaga, así que un
  aviso tardío nunca abre un personaje que nadie pidió.
- Un agitado genera varios avisos seguidos del sensor. Como el comando no admite
  ejecuciones simultáneas, mientras se abre un personaje los avisos extra se
  ignoran: nunca se apilan varios detalles.
- El id se elige entre 1 y el total del catálogo que informa la API. Ese total
  solo se toma del listado completo: con un filtro activo (por ejemplo, 287
  muertos) quedaría afuera más de la mitad de los personajes.
- No requiere permiso en tiempo de ejecución: Android e iOS consideran el
  acelerómetro un sensor no sensible.

### Interacción

Las animaciones necesitan acceso al control visual, que el ViewModel no debe
tener. Por eso se implementan como **Behaviors** declarados en el XAML:

- `AparicionGradualBehavior`: las tarjetas entran con un fundido y un leve
  desplazamiento.
- `PulsoAlTocarBehavior`: la estrella se agranda y rebota al tocarla.

Al marcar un favorito el teléfono vibra brevemente. En dispositivos sin motor de
vibración (una PC) el pedido se ignora sin afectar la acción.

La tarjeta está definida una sola vez en `Vistas/Plantillas` y la usan tanto la
lista como la pestaña de favoritos.

---

## Pruebas

El proyecto `tests/ExploradorPersonajes.Tests` usa **xUnit** y **Moq**, y
referencia solo al núcleo. Las pruebas siguen la estructura *Arrange, Act,
Assert* y se nombran `Metodo_Escenario_ResultadoEsperado`.

```
tests/ExploradorPersonajes.Tests/
├── Dobles/        Reemplazos de la red, datos de ejemplo y mocks compartidos
├── Modelos/       Mapeo del JSON y propiedades calculadas
├── Servicios/     Clasificación de errores, caché y ResultadoApi
├── Integracion/   Repositorios y servicio de favoritos contra SQLite real en memoria
├── Validaciones/  Reglas comunes y validador de avistamientos
└── ViewModels/    Estado de cada pantalla al ejecutar sus comandos
```

| Qué se prueba | Cómo se aísla |
|---|---|
| `ServicioPersonajes`: que 200, 404, 4xx, 5xx, JSON dañado, timeout y falta de red terminen en el tipo de error correcto, y cuándo se usa la copia guardada | `ManejadorHttpFalso` reemplaza la red; conectividad y caché son mocks |
| `ServicioCache`: guardar, leer y reemplazar copias | Una carpeta temporal por prueba, borrada al terminar |
| **Integración**: los repositorios de favoritos y avistamientos y el servicio de favoritos contra SQLite real (alta, baja, orden, duplicados, actualización, migración desde la versión anterior y persistencia al reabrir) | Base SQLite **en memoria**, con un nombre único por prueba para que las que corren en paralelo no compartan datos |
| Personaje al azar: el agitado abre un solo detalle aunque el sensor avise varias veces, el rango de ids sale del catálogo y no de una búsqueda filtrada, el sensor se enciende y apaga con la pantalla | El acelerómetro es un mock cuyo evento se dispara desde la prueba; el azar se fija para saber qué id esperar |
| Formulario de avistamiento: permisos concedidos y negados, dispositivo sin cámara, GPS sin respuesta, foto reemplazada o cancelada, errores por campo, guardado con vibración y aviso al detalle, y qué pasa con la foto si la base falla | Permisos, cámara, GPS, base y navegación son mocks |
| Validaciones: cada regla en sus valores límite (2, 3, 200 y 201 caracteres; ±90 y ±180 grados) y que se informen todos los errores juntos | No necesitan aislamiento: son funciones puras |
| ViewModels: mensajes, lista, paginación, búsqueda con espera, descarte de respuestas viejas, favoritos y vibración | La API, los favoritos, la navegación y la vibración son mocks |

La red no se usa nunca: así las pruebas son rápidas, no fallan si la API está
caída y pueden provocar errores (un 500, un timeout) que en la API real no se
pueden generar a voluntad.

---

## Integración continua

El workflow [`.github/workflows/explorador.yml`](../.github/workflows/explorador.yml)
corre en GitHub Actions con cada push o pull request que modifique esta
carpeta, y también se puede lanzar a mano.

```
push ──► Pruebas (ubuntu) ──┬──► Compilar Android (ubuntu)  ──► APK firmado
                            └──► Compilar Windows (windows) ──► carpeta de la app
```

| Etapa | Qué hace |
|---|---|
| **Pruebas** | Restaura, compila y ejecuta `dotnet test`. Publica los resultados (`.trx`) como artefacto, incluso si alguna prueba falla. |
| **Compilar** | Solo corre si las pruebas pasan. Una **matriz** genera Android y Windows en paralelo, cada una en su sistema operativo, con `dotnet publish` en Release. |
| **Artefactos** | El APK y la versión de Windows quedan para descargar desde la ejecución durante 14 días. |
| **Caché** | Los paquetes NuGet se guardan entre ejecuciones; la clave cambia solo si cambia algún `.csproj`. |

Decisiones:

- Las pruebas corren sin instalar MAUI, porque solo dependen del núcleo. Así
  la etapa que más se repite es también la más rápida.
- El filtro por rutas evita que un cambio en otra actividad del repositorio
  dispare este workflow.
- El número de ejecución se usa como `ApplicationVersion`: cada APK generado
  tiene un número de versión mayor que el anterior, como exige Android para
  instalarlo encima.
- Si llega un push nuevo mientras corre uno anterior, el anterior se cancela.

---

## Funcionalidades

- Carga de personajes desde la API con botón explícito
- Búsqueda por nombre con *debounce* de 500 ms
- Filtro por estado (vivos, muertos, desconocidos)
- Desplazamiento infinito sobre las 42 páginas del listado
- Recarga por gesto de arrastrar hacia abajo
- Pantalla de detalle con cinco propiedades del personaje
- Favoritos persistentes: con la estrella, deslizando la tarjeta o desde el detalle
- Pestaña de favoritos cargada en una sola llamada
- Copia local para seguir mostrando datos sin conexión
- Mensajes de estado diferenciados según el tipo de error
- Avistamientos con foto (cámara o galería), nota y ubicación GPS opcional
- Permisos pedidos en el momento justo, con alternativas si se niegan
- Agitar el teléfono (o tocar 🎲) para abrir un personaje al azar

El filtrado y la búsqueda se delegan al servidor, que acepta `name` y `status`
como parámetros de consulta. Filtrar en memoria solo alcanzaría a los registros
ya descargados, no a los 826 del catálogo.
