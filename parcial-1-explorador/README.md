# Explorador de Personajes

Aplicación **.NET MAUI** desarrollada como Parte B del primer examen parcial de
*Desarrollo de Aplicaciones Móviles 2*.

Consume la API pública de [Rick and Morty](https://rickandmortyapi.com/) y
permite listar, buscar y filtrar personajes, además de consultar el detalle de
cada uno.

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
├── Modelos/       Objetos de datos y mapeo del JSON
├── Servicios/     Acceso a la API y clasificación de errores
├── ViewModels/    Estado y comandos de cada pantalla
├── Vistas/        XAML, sin lógica en el code-behind
├── AppShell.xaml  Rutas de navegación
└── MauiProgram.cs Registro de dependencias
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

### Servicios

`ServicioPersonajes` es el único punto de la aplicación que habla con la red.

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

El botón **Probar 404** de la pantalla principal pide a propósito el personaje
con id 9999, que no existe, para poder demostrar el comportamiento.

### Navegación

Se usa **Shell** con rutas registradas (`Routing.RegisterRoute`). La navegación
al detalle se dispara desde el ViewModel y transporta el personaje seleccionado
como parámetro.

`DetalleViewModel` implementa `IQueryAttributable`, la interfaz que Shell invoca
al completar la navegación. Se eligió por sobre el atributo `[QueryProperty]`
porque hace explícito en el código dónde y cómo se recibe el parámetro, en lugar
de depender de un mapeo por reflexión.

### CommunityToolkit.MVVM

Se usa el Toolkit. `ObservableObject` evita implementar `INotifyPropertyChanged`
a mano, y los atributos `[ObservableProperty]` y `[RelayCommand]` generan las
propiedades con notificación y los comandos en tiempo de compilación.

`AllowConcurrentExecutions = false` en los comandos impide que dos toques
seguidos al mismo botón disparen dos llamadas de red simultáneas.

La contrapartida es que agrega una dependencia externa y que el código generado
no está a la vista, lo que dificulta el seguimiento paso a paso durante la
depuración.

---

## Funcionalidades

- Carga de personajes desde la API con botón explícito
- Búsqueda por nombre con *debounce* de 500 ms
- Filtro por estado (vivos, muertos, desconocidos)
- Desplazamiento infinito sobre las 42 páginas del listado
- Recarga por gesto de arrastrar hacia abajo
- Pantalla de detalle con cinco propiedades del personaje
- Mensajes de estado diferenciados según el tipo de error

El filtrado y la búsqueda se delegan al servidor, que acepta `name` y `status`
como parámetros de consulta. Filtrar en memoria sólo alcanzaría a los registros
ya descargados, no a los 826 del catálogo.
