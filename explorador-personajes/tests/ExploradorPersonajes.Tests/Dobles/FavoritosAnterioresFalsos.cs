using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Tests.Dobles;

/// <summary>
/// Fake de los favoritos de la versión anterior: una lista en memoria que se
/// vacía al borrarla, igual que la implementación real con Preferences.
/// </summary>
public class FavoritosAnterioresFalsos : IFavoritosAnteriores
{
    private readonly List<int> _ids;

    public FavoritosAnterioresFalsos(params int[] ids)
    {
        _ids = ids.ToList();
    }

    public int VecesBorrado { get; private set; }

    public IReadOnlyList<int> LeerIds() => _ids.ToList();

    public void Borrar()
    {
        _ids.Clear();
        VecesBorrado++;
    }
}
