using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Servicios;

/// <summary>Posición actual del dispositivo.</summary>
public interface IServicioUbicacion
{
    /// <summary>
    /// Devuelve la ubicación, o null si no se pudo obtener: GPS apagado, sin
    /// señal dentro del tiempo de espera, o dispositivo sin geolocalización.
    /// Debe llamarse con el permiso ya concedido.
    /// </summary>
    Task<Coordenadas?> ObtenerAsync(CancellationToken ct = default);
}
