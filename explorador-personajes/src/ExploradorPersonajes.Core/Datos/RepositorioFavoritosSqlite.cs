using ExploradorPersonajes.Modelos;
using SQLite;

namespace ExploradorPersonajes.Datos;

/// <summary>
/// Implementación con SQLite (biblioteca sqlite-net). Recibe la conexión ya
/// creada: la aplicación le pasa una sobre un archivo en la carpeta de datos
/// y las pruebas de integración, una sobre una base en memoria.
/// </summary>
public class RepositorioFavoritosSqlite : IRepositorioFavoritos
{
    private readonly SQLiteAsyncConnection _conexion;
    private readonly Func<DateTime> _ahora;
    private Task? _inicializacion;

    /// <param name="ahora">
    /// Reloj inyectable para que las pruebas puedan fijar la fecha de alta.
    /// Por defecto, la hora del dispositivo.
    /// </param>
    public RepositorioFavoritosSqlite(SQLiteAsyncConnection conexion, Func<DateTime>? ahora = null)
    {
        _conexion = conexion;
        _ahora = ahora ?? (() => DateTime.Now);
    }

    /// <summary>
    /// Crea la tabla la primera vez que se usa el repositorio, no en el
    /// constructor: así crear el objeto no toca el disco y la tabla se crea
    /// una sola vez aunque lleguen varias operaciones juntas.
    /// </summary>
    private async Task<SQLiteAsyncConnection> ConexionAsync()
    {
        _inicializacion ??= _conexion.CreateTableAsync<FavoritoEntidad>();
        await _inicializacion;
        return _conexion;
    }

    public async Task<List<FavoritoEntidad>> ObtenerTodosAsync()
    {
        var db = await ConexionAsync();
        return await db.Table<FavoritoEntidad>().OrderBy(f => f.Orden).ToListAsync();
    }

    public async Task AgregarAsync(Personaje personaje)
    {
        var db = await ConexionAsync();

        // Se hace dentro de una transacción para que consultar el último
        // orden e insertar sean una sola operación indivisible.
        await db.RunInTransactionAsync(conexion =>
        {
            if (conexion.Find<FavoritoEntidad>(personaje.Id) is not null)
            {
                return;
            }

            var ultimoOrden = conexion.ExecuteScalar<int>("SELECT IFNULL(MAX(Orden), 0) FROM favoritos");
            conexion.Insert(FavoritoEntidad.DesdePersonaje(personaje, ultimoOrden + 1, _ahora()));
        });
    }

    public async Task QuitarAsync(int id)
    {
        var db = await ConexionAsync();
        await db.DeleteAsync<FavoritoEntidad>(id);
    }

    public async Task ActualizarDatosAsync(IEnumerable<Personaje> personajes)
    {
        var db = await ConexionAsync();
        var lista = personajes.ToList();

        await db.RunInTransactionAsync(conexion =>
        {
            foreach (var personaje in lista)
            {
                var existente = conexion.Find<FavoritoEntidad>(personaje.Id);
                if (existente is null)
                {
                    continue;
                }

                conexion.Update(FavoritoEntidad.DesdePersonaje(personaje, existente.Orden, existente.FechaAlta));
            }
        });
    }
}
