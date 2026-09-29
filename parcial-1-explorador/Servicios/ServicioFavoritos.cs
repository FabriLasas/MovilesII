using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Mensajes;
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

/// <summary>
/// Persiste sólo los ids, no los personajes completos: los datos se piden a
/// la API al mostrarlos, así que siempre están actualizados. Una lista de
/// números es exactamente el tipo de valor chico para el que existe
/// Preferences.
/// </summary>
public class ServicioFavoritos : IServicioFavoritos
{
    private const string Clave = "favoritos";

    private readonly IPreferences _preferencias;
    private readonly IMessenger _mensajero;
    private readonly List<int> _ids;

    public ServicioFavoritos(IPreferences preferencias, IMessenger mensajero)
    {
        _preferencias = preferencias;
        _mensajero = mensajero;
        _ids = Leer();
    }

    public bool EsFavorito(int id) => _ids.Contains(id);

    public IReadOnlyList<int> ObtenerIds() => _ids.AsReadOnly();

    public bool Alternar(Personaje personaje)
    {
        var esFavorito = !_ids.Remove(personaje.Id);
        if (esFavorito)
        {
            _ids.Add(personaje.Id);
        }

        _preferencias.Set(Clave, string.Join(",", _ids));

        // El servicio publica el cambio, así todas las pantallas se enteran
        // sin importar desde cuál se tocó la estrella.
        _mensajero.Send(new FavoritoCambiadoMensaje(personaje, esFavorito));

        return esFavorito;
    }

    /// <summary>
    /// Se guarda como texto "1,5,12". Al leer se descartan los valores que no
    /// sean números, por si el dato quedó dañado.
    /// </summary>
    private List<int> Leer()
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
}
