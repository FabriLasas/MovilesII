using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Datos;

/// <summary>
/// Acceso a los favoritos guardados en el dispositivo. El servicio de
/// favoritos depende de esta interfaz y no de SQLite: en las pruebas
/// unitarias se reemplaza por un mock, y en las de integración se usa la
/// implementación real sobre una base en memoria.
/// </summary>
public interface IRepositorioFavoritos
{
    /// <summary>Todos los favoritos, en el orden en que se marcaron.</summary>
    Task<List<FavoritoEntidad>> ObtenerTodosAsync();

    /// <summary>
    /// Agrega el personaje al final de la lista. Si ya estaba, no lo duplica
    /// ni cambia su posición.
    /// </summary>
    Task AgregarAsync(Personaje personaje);

    Task QuitarAsync(int id);

    /// <summary>
    /// Reemplaza los datos guardados por los recién traídos de la API (por
    /// ejemplo, si un personaje cambió de ubicación), conservando el orden y
    /// la fecha de alta. Ignora los que ya no son favoritos.
    /// </summary>
    Task ActualizarDatosAsync(IEnumerable<Personaje> personajes);
}
