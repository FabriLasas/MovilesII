using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Administra los personajes marcados como favoritos.
/// </summary>
public interface IServicioFavoritos
{
    bool EsFavorito(int id);

    /// <summary>Marca o desmarca el personaje. Devuelve el estado nuevo.</summary>
    bool Alternar(Personaje personaje);

    /// <summary>Ids en el orden en que el usuario los fue marcando.</summary>
    IReadOnlyList<int> ObtenerIds();
}
