using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Ubicación con la API Geolocation de MAUI.
/// </summary>
public class ServicioUbicacion : IServicioUbicacion
{
    /// <summary>
    /// Precisión media (wifi + GPS asistido): alcanza para saber dónde se tomó
    /// la foto y gasta mucha menos batería que la alta. Diez segundos como
    /// máximo para no dejar al usuario esperando sin señal.
    /// </summary>
    private static readonly GeolocationRequest Pedido = new(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

    private readonly IGeolocation _geolocalizacion;

    public ServicioUbicacion(IGeolocation geolocalizacion)
    {
        _geolocalizacion = geolocalizacion;
    }

    public async Task<Coordenadas?> ObtenerAsync(CancellationToken ct = default)
    {
        try
        {
            // Primero una lectura nueva; si no llega a tiempo, la última
            // conocida por el sistema (sin encender el GPS) es mejor que nada.
            var ubicacion = await _geolocalizacion.GetLocationAsync(Pedido, ct)
                            ?? await _geolocalizacion.GetLastKnownLocationAsync();

            return ubicacion is null ? null : new Coordenadas(ubicacion.Latitude, ubicacion.Longitude);
        }
        catch (FeatureNotEnabledException)
        {
            // El GPS está apagado en el dispositivo.
            return null;
        }
        catch (FeatureNotSupportedException)
        {
            return null;
        }
        catch (PermissionException)
        {
            return null;
        }
    }
}
