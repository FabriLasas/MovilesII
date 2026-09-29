using System.Net;
using System.Text.Json;
using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Implementación del acceso a la API pública de Rick and Morty.
///
/// Decisión de diseño: NO se usa EnsureSuccessStatusCode(). Ese método
/// convierte cualquier respuesta no exitosa en una HttpRequestException,
/// que es la MISMA excepción que se lanza cuando no hay red. Al unificarlas
/// se pierde la información necesaria para dar mensajes distintos, que es
/// justamente lo que se quiere lograr. En su lugar se inspecciona
/// StatusCode y se capturan por separado las excepciones de transporte.
/// </summary>
public class ServicioPersonajes : IServicioPersonajes
{
    private const string UrlBase = "https://rickandmortyapi.com/api/";

    private readonly HttpClient _http;
    private readonly IServicioConectividad _conectividad;
    private readonly IServicioCache _cache;

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ServicioPersonajes(HttpClient http, IServicioConectividad conectividad, IServicioCache cache)
    {
        _http = http;
        _conectividad = conectividad;
        _cache = cache;
        _http.BaseAddress = new Uri(UrlBase);
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public Task<ResultadoApi<RespuestaPaginada>> ObtenerPersonajesAsync(
        int pagina = 1,
        string? nombre = null,
        string? estado = null,
        CancellationToken ct = default)
        => EjecutarAsync<RespuestaPaginada>(ConstruirRuta(pagina, nombre, estado), esBusqueda: true, ct);

    public Task<ResultadoApi<Personaje>> ObtenerPersonajePorIdAsync(int id, CancellationToken ct = default)
        => EjecutarAsync<Personaje>($"character/{id}", esBusqueda: false, ct);

    public async Task<ResultadoApi<List<Personaje>>> ObtenerPersonajesPorIdsAsync(
        IReadOnlyList<int> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return ResultadoApi<List<Personaje>>.Exito(new List<Personaje>());
        }

        // Particularidad de la API: con un solo id devuelve un OBJETO, y con
        // varios devuelve un ARRAY. Deserializar un objeto como lista falla,
        // así que el caso de un único favorito se resuelve por separado.
        if (ids.Count == 1)
        {
            var unico = await ObtenerPersonajePorIdAsync(ids[0], ct);

            // Si el único favorito ya no existe, no es un error para el
            // usuario: simplemente no hay nada que mostrar.
            if (unico.Error == TipoError.NoEncontrado)
            {
                return ResultadoApi<List<Personaje>>.Exito(new List<Personaje>());
            }

            return unico.Transformar(personaje => new List<Personaje> { personaje });
        }

        var resultado = await EjecutarAsync<List<Personaje>>(
            $"character/{string.Join(",", ids)}",
            esBusqueda: false,
            ct);

        // La API devuelve los personajes ordenados por id, no en el orden
        // pedido, y omite en silencio los ids inexistentes. Se reordena para
        // respetar el orden en que el usuario marcó sus favoritos.
        return resultado.Transformar(lista =>
        {
            var porId = lista.ToDictionary(p => p.Id);
            return ids.Where(porId.ContainsKey).Select(id => porId[id]).ToList();
        });
    }

    /// <summary>
    /// Arma la cadena de consulta omitiendo los filtros vacíos. Se escapa el
    /// nombre porque el usuario puede escribir espacios o acentos, que no son
    /// válidos sin codificar dentro de una URL.
    /// </summary>
    private static string ConstruirRuta(int pagina, string? nombre, string? estado)
    {
        var parametros = new List<string> { $"page={pagina}" };

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            parametros.Add($"name={Uri.EscapeDataString(nombre.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            parametros.Add($"status={Uri.EscapeDataString(estado)}");
        }

        return $"character/?{string.Join("&", parametros)}";
    }

    private const string MensajeSinConexion =
        "No hay conexión a internet. Verificá el wifi o los datos móviles.";

    /// <summary>
    /// Punto único de entrada a la red. Centralizar aquí el manejo de errores
    /// evita repetir bloques try/catch en cada método y garantiza que todas
    /// las llamadas clasifiquen los fallos con el mismo criterio.
    /// </summary>
    /// <param name="esBusqueda">
    /// Distingue el significado del 404. En un listado o búsqueda, la API
    /// responde 404 cuando no hay coincidencias: eso no es un error sino un
    /// resultado vacío. Al pedir un recurso puntual por id, el mismo 404 sí
    /// indica que el recurso no existe.
    /// </param>
    private async Task<ResultadoApi<T>> EjecutarAsync<T>(string rutaRelativa, bool esBusqueda, CancellationToken ct)
    {
        // 1) Chequeo previo de conectividad: sólo si hay certeza de que no hay
        //    red se evita la llamada, para responder al instante en lugar de
        //    esperar el timeout. En los casos dudosos se intenta igual.
        if (_conectividad.SinRedConfirmada)
        {
            return await RespaldoDesdeCacheAsync<T>(rutaRelativa, TipoError.SinConexion, MensajeSinConexion);
        }

        try
        {
            using var respuesta = await _http.GetAsync(rutaRelativa, ct);

            // 2) La respuesta llegó: el problema, si lo hay, es del lado HTTP.
            if (!respuesta.IsSuccessStatusCode)
            {
                return ClasificarErrorHttp<T>(respuesta.StatusCode, esBusqueda);
            }

            // 3) Respuesta exitosa: queda validar que el cuerpo sea el esperado.
            var contenido = await respuesta.Content.ReadAsStringAsync(ct);
            var datos = JsonSerializer.Deserialize<T>(contenido, OpcionesJson);

            if (datos is null)
            {
                return ResultadoApi<T>.Fallo(
                    TipoError.RespuestaInvalida,
                    "El servidor respondió con un formato inesperado.");
            }

            // 4) Sólo se guarda en caché lo que se pudo interpretar bien, para
            //    no terminar mostrando sin conexión una respuesta dañada.
            await _cache.GuardarAsync(rutaRelativa, contenido);

            return ResultadoApi<T>.Exito(datos);
        }
        catch (JsonException)
        {
            // Llegó un 200 pero el cuerpo no es el JSON que esperábamos.
            return ResultadoApi<T>.Fallo(
                TipoError.RespuestaInvalida,
                "No se pudieron interpretar los datos recibidos.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout del HttpClient. Se descarta el caso en que la cancelación
            // la pidió el propio usuario, que no es un error a mostrar.
            return await RespaldoDesdeCacheAsync<T>(
                rutaRelativa,
                TipoError.TiempoAgotado,
                "El servidor tardó demasiado en responder. Intentá de nuevo.");
        }
        catch (HttpRequestException ex)
        {
            // Fallo de transporte. Recién acá se consulta al sistema, pero sólo
            // para explicar el fallo: si además no informa internet, el
            // problema más probable es la conexión del dispositivo; si la
            // informa, es el servidor el que no responde (DNS, caída, certificado).
            return _conectividad.SistemaInformaInternet
                ? await RespaldoDesdeCacheAsync<T>(
                    rutaRelativa,
                    TipoError.FalloDeRed,
                    $"No se pudo contactar al servidor. ({ex.Message})")
                : await RespaldoDesdeCacheAsync<T>(rutaRelativa, TipoError.SinConexion, MensajeSinConexion);
        }
    }

    /// <summary>
    /// Ante un problema de red, intenta responder con la última copia guardada
    /// de esa misma consulta. Sólo se usa para fallos de conectividad: un 404
    /// o un 500 son respuestas reales del servidor y se informan tal cual.
    /// </summary>
    private async Task<ResultadoApi<T>> RespaldoDesdeCacheAsync<T>(string rutaRelativa, TipoError motivo, string mensaje)
    {
        var entrada = await _cache.LeerAsync(rutaRelativa);
        if (entrada is null)
        {
            return ResultadoApi<T>.Fallo(motivo, mensaje);
        }

        try
        {
            var datos = JsonSerializer.Deserialize<T>(entrada.Contenido, OpcionesJson);
            return datos is null
                ? ResultadoApi<T>.Fallo(motivo, mensaje)
                : ResultadoApi<T>.ExitoDesdeCache(datos, entrada.Fecha, motivo, mensaje);
        }
        catch (JsonException)
        {
            return ResultadoApi<T>.Fallo(motivo, mensaje);
        }
    }

    private static ResultadoApi<T> ClasificarErrorHttp<T>(HttpStatusCode codigo, bool esBusqueda)
    {
        var numero = (int)codigo;

        if (codigo == HttpStatusCode.NotFound)
        {
            return esBusqueda
                ? ResultadoApi<T>.Fallo(TipoError.SinResultados,
                    "No se encontraron personajes para esa búsqueda.", numero)
                : ResultadoApi<T>.Fallo(TipoError.NoEncontrado,
                    "El personaje solicitado no existe. (Error 404)", numero);
        }

        if (numero >= 500)
        {
            return ResultadoApi<T>.Fallo(TipoError.ErrorServidor,
                $"El servidor tuvo un problema interno. (Error {numero})", numero);
        }

        return ResultadoApi<T>.Fallo(TipoError.ErrorCliente,
            $"La solicitud fue rechazada por el servidor. (Error {numero})", numero);
    }
}
