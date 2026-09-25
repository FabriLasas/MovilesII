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

    public static ResultadoApi<T> Exito(T datos) => new()
    {
        EsExitoso = true,
        Datos = datos,
        Error = TipoError.Ninguno
    };

    public static ResultadoApi<T> Fallo(TipoError error, string mensaje, int? codigoHttp = null) => new()
    {
        EsExitoso = false,
        Error = error,
        Mensaje = mensaje,
        CodigoHttp = codigoHttp
    };
}
