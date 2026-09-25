using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Abstracción del acceso a datos. El ViewModel depende de esta interfaz y no
/// de la implementación concreta, lo que permite reemplazar la fuente de datos
/// o inyectar un doble de prueba sin tocar la lógica de presentación.
/// </summary>
public interface IServicioPersonajes
{
    /// <summary>
    /// Obtiene una página de personajes, opcionalmente filtrada.
    /// El filtrado se delega al servidor (la API acepta name y status como
    /// parámetros de consulta) en lugar de traer todo y filtrar en memoria:
    /// se transfieren menos datos y funciona sobre los 826 registros, no sólo
    /// sobre los ya descargados.
    /// </summary>
    /// <param name="pagina">Número de página, base 1.</param>
    /// <param name="nombre">Texto a buscar en el nombre, o null para no filtrar.</param>
    /// <param name="estado">Valor de estado que espera la API (alive, dead, unknown), o null.</param>
    Task<ResultadoApi<RespuestaPaginada>> ObtenerPersonajesAsync(
        int pagina = 1,
        string? nombre = null,
        string? estado = null,
        CancellationToken ct = default);

    Task<ResultadoApi<Personaje>> ObtenerPersonajePorIdAsync(int id, CancellationToken ct = default);
}
