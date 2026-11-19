# Explorador de Personajes

[![Explorador de Personajes](https://github.com/FabriLasas/MovilesII/actions/workflows/explorador.yml/badge.svg)](https://github.com/FabriLasas/MovilesII/actions/workflows/explorador.yml)

Aplicación **.NET MAUI** desarrollada como Parte B del primer examen parcial de
*Desarrollo de Aplicaciones Móviles 2*.

Consume la API pública de [Rick and Morty](https://rickandmortyapi.com/) y
permite listar, buscar y filtrar personajes, consultar el detalle de cada uno y
guardar favoritos. Sigue mostrando datos sin conexión a partir de una copia
local.

> La versión entregada en el **Parcial 1** quedó congelada en la etiqueta
> [`parcial-1`](https://github.com/FabriLasas/MovilesII/tree/parcial-1/parcial-1-explorador).
> Esta carpeta contiene la evolución del proyecto para el **Parcial 2**.

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

Las dos filas con 404 merecen atención: la API responde con ese mismo código
tanto cuando una búsqueda no arroja resultados como cuando se pide un personaje
que no existe. En el primer caso no es un error sino un resultado vacío, y se
informa sin formato de alerta. El servicio distingue ambos casos según la
operación que se haya solicitado.

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
intenta la petición y la señal del sistema sólo se usa para explicar un fallo.

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

**Cambio respecto del Parcial 1.** Antes se guardaban sólo los ids en
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
| Omite en silencio los ids inexistentes | Se muestran sólo los que llegaron |

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
agrega la interfaz y sólo delega. Se eligió `IQueryAttributable` por sobre el
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
referencia sólo al núcleo. Las pruebas siguen la estructura *Arrange, Act,
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
| **Compilar** | Sólo corre si las pruebas pasan. Una **matriz** genera Android y Windows en paralelo, cada una en su sistema operativo, con `dotnet publish` en Release. |
| **Artefactos** | El APK y la versión de Windows quedan para descargar desde la ejecución durante 14 días. |
| **Caché** | Los paquetes NuGet se guardan entre ejecuciones; la clave cambia sólo si cambia algún `.csproj`. |

Decisiones:

- Las pruebas corren sin instalar MAUI, porque sólo dependen del núcleo. Así
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

El filtrado y la búsqueda se delegan al servidor, que acepta `name` y `status`
como parámetros de consulta. Filtrar en memoria sólo alcanzaría a los registros
ya descargados, no a los 826 del catálogo.
