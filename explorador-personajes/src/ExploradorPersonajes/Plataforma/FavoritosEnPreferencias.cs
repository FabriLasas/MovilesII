using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Lee los favoritos con el formato de la versión del Parcial 1: los ids como
/// texto "1,5,12" en Preferences. Solo se usa para pasarlos a la base de
/// datos la primera vez que se abre la versión nueva.
/// </summary>
public class FavoritosEnPreferencias : IFavoritosAnteriores
{
    private const string Clave = "favoritos";

    private readonly IPreferences _preferencias;

    public FavoritosEnPreferencias(IPreferences preferencias)
    {
        _preferencias = preferencias;
    }

    /// <summary>
    /// Descarta los valores que no sean números, por si el dato quedó dañado,
    /// y los repetidos.
    /// </summary>
    public IReadOnlyList<int> LeerIds()
    {
        var texto = _preferencias.Get(Clave, string.Empty);

        return texto
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(parte => int.TryParse(parte, out var id) ? id : (int?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
    }

    public void Borrar() => _preferencias.Remove(Clave);
}
