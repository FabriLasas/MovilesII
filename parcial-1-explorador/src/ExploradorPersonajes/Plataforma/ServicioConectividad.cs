using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Implementación con la API de conectividad de MAUI.
///
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
