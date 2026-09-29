namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Informa el estado de la red. Se abstrae en una interfaz en lugar de
/// consultar Connectivity.Current directamente por dos motivos: poder simular
/// la falta de red (para demostrar el modo sin conexión sin apagar el wifi) y
/// concentrar en un solo lugar el criterio de cuándo confiar en el sistema.
/// </summary>
public interface IServicioConectividad
{
    /// <summary>
    /// Hay certeza de que no hay red: se está simulando el modo sin conexión o
    /// el sistema no detecta ninguna red en absoluto. Sólo en este caso vale
    /// la pena no intentar la petición.
    /// </summary>
    bool SinRedConfirmada { get; }

    /// <summary>
    /// El sistema informa acceso pleno a internet. Esta señal puede fallar
    /// momentáneamente estando conectado, así que se usa para explicar un
    /// fallo de red, nunca para impedir una petición.
    /// </summary>
    bool SistemaInformaInternet { get; }

    /// <summary>
    /// Fuerza a la aplicación a comportarse como si no hubiera red.
    /// Pensado para demostrar el manejo de errores y la caché.
    /// </summary>
    bool SimularSinConexion { get; set; }
}

/// <summary>
/// Decisión de diseño: durante las pruebas, Windows informó por un momento que
/// no había internet estando conectado, y la aplicación mostró "sin conexión"
/// sin siquiera intentar la petición. Por eso la señal del sistema dejó de
/// usarse como filtro previo salvo cuando es inequívoca (ninguna red).
/// </summary>
public class ServicioConectividad : IServicioConectividad
{
    private readonly IConnectivity _conectividad;

    public ServicioConectividad(IConnectivity conectividad)
    {
        _conectividad = conectividad;
    }

    public bool SimularSinConexion { get; set; }

    public bool SinRedConfirmada =>
        SimularSinConexion || _conectividad.NetworkAccess == NetworkAccess.None;

    public bool SistemaInformaInternet =>
        !SimularSinConexion && _conectividad.NetworkAccess == NetworkAccess.Internet;
}
