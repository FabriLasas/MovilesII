# Explorador de Personajes

Aplicación **.NET MAUI** desarrollada como Parte B del primer examen parcial de
*Desarrollo de Aplicaciones Móviles 2*.

Consume la API pública de [Rick and Morty](https://rickandmortyapi.com/) y
permite listar, buscar y filtrar personajes, consultar el detalle de cada uno y
guardar favoritos. Sigue mostrando datos sin conexión a partir de una copia
local.

---

## Cómo ejecutar

Requiere el **SDK de .NET 10** con las cargas de trabajo de MAUI instaladas.

```bash
dotnet restore
```

En Windows:

```bash
dotnet build -f net10.0-windows10.0.19041.0
```

En Android (con un emulador o dispositivo conectado):

```bash
dotnet build -t:Run -f net10.0-android
```

---

## Arquitectura

El proyecto aplica el patrón **MVVM**. Cada carpeta corresponde a una
responsabilidad y las dependencias apuntan siempre hacia adentro: las vistas
conocen a los ViewModels, los ViewModels conocen la interfaz del servicio, y
nadie conoce a las vistas.

```
ExploradorPersonajes/
├── Modelos/          Objetos de datos y mapeo del JSON
├── Servicios/        API, caché, favoritos, conectividad y navegación
├── ViewModels/       Estado y comandos de cada pantalla y de cada tarjeta
├── Vistas/           XAML, sin lógica en el code-behind
│   └── Plantillas/   Tarjeta de personaje compartida entre pantallas
├── Comportamientos/  Animaciones reutilizables declaradas desde XAML
├── Mensajes/         Avisos entre pantallas (favorito agregado o quitado)
├── AppShell.xaml     Pestañas y rutas de navegación
└── MauiProgram.cs    Registro de dependencias
```

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
| `ServicioFavoritos` | Persiste los ids favoritos con `Preferences` |
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

Se guardan sólo los ids, en `Preferences`: los datos se piden a la API al
mostrarlos, así que siempre están actualizados.

La pestaña los trae en **una sola llamada** (`/character/1,5,12`). El endpoint
tiene tres particularidades, verificadas contra la API:

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

`DetalleViewModel` implementa `IQueryAttributable`, la interfaz que Shell invoca
al completar la navegación. Se eligió por sobre el atributo `[QueryProperty]`
porque hace explícito en el código dónde y cómo se recibe el parámetro, en lugar
de depender de un mapeo por reflexión.

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
