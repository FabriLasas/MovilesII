namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Favoritos guardados por la versión anterior de la aplicación (Parcial 1),
/// que solo guardaba los ids como texto en las preferencias del dispositivo.
/// Se leen una única vez para pasarlos a la base de datos.
/// </summary>
public interface IFavoritosAnteriores
{
    IReadOnlyList<int> LeerIds();

    void Borrar();
}
