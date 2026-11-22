using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Favoritos guardados en SQLite.
///
/// Cambio respecto del Parcial 1: antes se guardaban solo los ids en
/// Preferences y los datos se pedían a la API cada vez, así que sin conexión
/// la pestaña Favoritos dependía de que hubiera una copia de esa consulta
/// exacta. Ahora cada favorito se guarda con sus datos y la pestaña funciona
/// siempre; la API solo se usa para refrescarlos.
/// </summary>
public class ServicioFavoritos : IServicioFavoritos
{
    private readonly IRepositorioFavoritos _repositorio;
    private readonly IFavoritosAnteriores _anteriores;
    private readonly IServicioPersonajes _api;
    private readonly IMessenger _mensajero;

    /// <summary>Copia en memoria de lo guardado, en orden.</summary>
    private readonly List<Personaje> _favoritos = new();

    private Task? _inicializacion;

    public ServicioFavoritos(
        IRepositorioFavoritos repositorio,
        IFavoritosAnteriores anteriores,
        IServicioPersonajes api,
        IMessenger mensajero)
    {
        _repositorio = repositorio;
        _anteriores = anteriores;
        _api = api;
        _mensajero = mensajero;
    }

    public async Task InicializarAsync()
    {
        _inicializacion ??= CargarAsync();

        try
        {
            await _inicializacion;
        }
        catch
        {
            // Si la carga falló, se descarta para que la próxima llamada la
            // reintente en lugar de devolver siempre el mismo error.
            _inicializacion = null;
            throw;
        }
    }

    private async Task CargarAsync()
    {
        await MigrarVersionAnteriorAsync();

        var guardados = await _repositorio.ObtenerTodosAsync();
        _favoritos.Clear();
        _favoritos.AddRange(guardados.Select(f => f.APersonaje()));
    }

    /// <summary>
    /// Pasa a la base los favoritos de la versión anterior. Como ésta solo
    /// guardaba ids, hay que pedir los datos a la API. Si falla (por ejemplo,
    /// sin conexión) los ids se conservan y se reintenta en el próximo inicio:
    /// el usuario nunca pierde un favorito por actualizar la aplicación.
    /// </summary>
    private async Task MigrarVersionAnteriorAsync()
    {
        var ids = _anteriores.LeerIds();
        if (ids.Count == 0)
        {
            return;
        }

        var resultado = await _api.ObtenerPersonajesPorIdsAsync(ids);
        if (!resultado.EsExitoso || resultado.Datos is null)
        {
            return;
        }

        foreach (var personaje in resultado.Datos)
        {
            await _repositorio.AgregarAsync(personaje);
        }

        _anteriores.Borrar();
    }

    public bool EsFavorito(int id) => _favoritos.Any(f => f.Id == id);

    public IReadOnlyList<Personaje> ObtenerTodos() => _favoritos.ToList();

    public async Task<bool> AlternarAsync(Personaje personaje)
    {
        await InicializarAsync();

        var posicion = _favoritos.FindIndex(f => f.Id == personaje.Id);
        var esFavorito = posicion < 0;

        // Primero la base y después la memoria: si guardar falla, la memoria
        // no queda mostrando un estado que no se guardó.
        if (esFavorito)
        {
            await _repositorio.AgregarAsync(personaje);
            _favoritos.Add(personaje);
        }
        else
        {
            await _repositorio.QuitarAsync(personaje.Id);
            _favoritos.RemoveAt(posicion);
        }

        // El servicio publica el cambio, así todas las pantallas se enteran
        // sin importar desde cuál se tocó la estrella.
        _mensajero.Send(new FavoritoCambiadoMensaje(personaje, esFavorito));

        return esFavorito;
    }

    public async Task ActualizarDatosAsync(IEnumerable<Personaje> personajes)
    {
        var lista = personajes.ToList();
        await _repositorio.ActualizarDatosAsync(lista);

        foreach (var actualizado in lista)
        {
            var posicion = _favoritos.FindIndex(f => f.Id == actualizado.Id);
            if (posicion >= 0)
            {
                _favoritos[posicion] = actualizado;
            }
        }
    }
}
