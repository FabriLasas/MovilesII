using SQLite;

namespace ExploradorPersonajes.Datos;

/// <summary>
/// Implementación con SQLite. Comparte la conexión (y el archivo de base)
/// con el repositorio de favoritos; cada uno crea su propia tabla.
/// </summary>
public class RepositorioAvistamientosSqlite : IRepositorioAvistamientos
{
    private readonly SQLiteAsyncConnection _conexion;
    private Task? _inicializacion;

    public RepositorioAvistamientosSqlite(SQLiteAsyncConnection conexion)
    {
        _conexion = conexion;
    }

    private async Task<SQLiteAsyncConnection> ConexionAsync()
    {
        _inicializacion ??= _conexion.CreateTableAsync<Avistamiento>();
        await _inicializacion;
        return _conexion;
    }

    public async Task GuardarAsync(Avistamiento avistamiento)
    {
        var db = await ConexionAsync();

        // InsertAsync completa el Id autoincremental en el mismo objeto.
        await db.InsertAsync(avistamiento);
    }

    public async Task<List<Avistamiento>> ObtenerPorPersonajeAsync(int personajeId)
    {
        var db = await ConexionAsync();

        // El Id desempata dos avistamientos con la misma fecha y hora.
        return await db.Table<Avistamiento>()
            .Where(a => a.PersonajeId == personajeId)
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .ToListAsync();
    }

    public async Task EliminarAsync(int id)
    {
        var db = await ConexionAsync();
        await db.DeleteAsync<Avistamiento>(id);
    }
}
