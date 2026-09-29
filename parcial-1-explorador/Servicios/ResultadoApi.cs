namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Clasificación de fallos. Permite que el ViewModel decida qué mensaje
/// mostrar sin conocer detalles de HttpClient ni de excepciones.
/// </summary>
public enum TipoError
{
    Ninguno,
    SinConexion,      // El dispositivo no tiene acceso a internet
    TiempoAgotado,    // El servidor no respondió dentro del timeout
    FalloDeRed,       // DNS caído, servidor inalcanzable, certificado inválido
    SinResultados,    // La búsqueda no arrojó coincidencias (404 "esperado")
    NoEncontrado,     // El recurso pedido no existe (404 "real")
    ErrorCliente,     // Otros 4xx
    ErrorServidor,    // 5xx
    RespuestaInvalida // Llegó un 200 pero el JSON no se pudo deserializar
}

/// <summary>
/// Envuelve el resultado de una llamada a la API. Evita lanzar excepciones
/// hacia el ViewModel: el flujo de error es un valor de retorno, no un salto
/// de control. Así la lógica de presentación queda lineal y testeable.
/// </summary>
public class ResultadoApi<T>
{
    public bool EsExitoso { get; private init; }
    public T? Datos { get; private init; }
    public TipoError Error { get; private init; }
    public string Mensaje { get; private init; } = string.Empty;
    public int? CodigoHttp { get; private init; }

    /// <summary>
    /// Indica que los datos no vienen del servidor sino de la copia local
    /// guardada en la última consulta exitosa. El resultado se considera
    /// exitoso (hay datos para mostrar), pero la vista debe avisarlo.
    /// </summary>
    public bool DesdeCache { get; private init; }

    /// <summary>Momento en que se guardó la copia local, si DesdeCache es true.</summary>
    public DateTime? FechaCache { get; private init; }

    public static ResultadoApi<T> Exito(T datos) => new()
    {
        EsExitoso = true,
        Datos = datos,
        Error = TipoError.Ninguno
    };

    /// <summary>
    /// Éxito obtenido de la caché tras un fallo de red. Conserva el tipo de
    /// error y el mensaje originales para que la vista pueda explicar por qué
    /// está mostrando datos guardados.
    /// </summary>
    public static ResultadoApi<T> ExitoDesdeCache(T datos, DateTime fecha, TipoError motivo, string mensaje) => new()
    {
        EsExitoso = true,
        Datos = datos,
        DesdeCache = true,
        FechaCache = fecha,
        Error = motivo,
        Mensaje = mensaje
    };

    public static ResultadoApi<T> Fallo(TipoError error, string mensaje, int? codigoHttp = null) => new()
    {
        EsExitoso = false,
        Error = error,
        Mensaje = mensaje,
        CodigoHttp = codigoHttp
    };

    /// <summary>
    /// Convierte los datos a otro tipo conservando el resto de la información
    /// (error, mensaje, origen en caché). Se usa, por ejemplo, para envolver
    /// un único personaje en una lista.
    /// </summary>
    public ResultadoApi<TNuevo> Transformar<TNuevo>(Func<T, TNuevo> conversion) => new()
    {
        EsExitoso = EsExitoso,
        Datos = EsExitoso && Datos is not null ? conversion(Datos) : default,
        Error = Error,
        Mensaje = Mensaje,
        CodigoHttp = CodigoHttp,
        DesdeCache = DesdeCache,
        FechaCache = FechaCache
    };
}
