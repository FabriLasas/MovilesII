using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Administra los personajes marcados como favoritos.
///
/// Mantiene una copia en memoria de lo guardado para que consultas como
/// <see cref="EsFavorito"/>, que se hacen al crear cada tarjeta, respondan
/// al instante sin ir a la base de datos.
/// </summary>
public interface IServicioFavoritos
{
    /// <summary>
    /// Carga los favoritos guardados. Se puede llamar las veces que haga
    /// falta: sólo la primera llamada trabaja, las demás esperan esa misma
    /// carga.
    /// </summary>
    Task InicializarAsync();

    bool EsFavorito(int id);

    /// <summary>Marca o desmarca el personaje y lo guarda. Devuelve el estado nuevo.</summary>
    Task<bool> AlternarAsync(Personaje personaje);

    /// <summary>Favoritos en el orden en que el usuario los fue marcando.</summary>
    IReadOnlyList<Personaje> ObtenerTodos();

    /// <summary>Reemplaza los datos guardados por los recién traídos de la API.</summary>
    Task ActualizarDatosAsync(IEnumerable<Personaje> personajes);
}
