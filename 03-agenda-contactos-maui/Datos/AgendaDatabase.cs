using System.Globalization;
using AgendaContactosMaui.Modelos;
using SQLite;

namespace AgendaContactosMaui.Datos;

/// <summary>
/// Capa de acceso a datos: encapsula la conexión a SQLite y las operaciones CRUD.
/// Los ViewModels no escriben SQL ni conocen la conexión, sólo llaman a estos métodos.
/// </summary>
public class AgendaDatabase
{
    public const int MaxNombre = 50;

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    /// <summary>
    /// La conexión se crea la primera vez que se usa (y una sola vez aunque dos
    /// llamadas lleguen juntas), así el arranque de la app no espera a la base.
    /// </summary>
    private readonly Lazy<Task<SQLiteAsyncConnection>> _conexion;

    public AgendaDatabase()
    {
        _conexion = new Lazy<Task<SQLiteAsyncConnection>>(InicializarAsync);
    }

    /// <summary>
    /// Paso 2: crea el archivo agenda.db3 en la carpeta de datos de la app y la
    /// tabla a partir de los atributos de <see cref="Contacto"/>.
    /// Paso 3a: si la tabla está vacía, inserta 3 contactos de ejemplo.
    /// </summary>
    private static async Task<SQLiteAsyncConnection> InicializarAsync()
    {
        var ruta = Path.Combine(FileSystem.AppDataDirectory, "agenda.db3");
        var db = new SQLiteAsyncConnection(ruta, Flags);

        await db.CreateTableAsync<Contacto>();

        if (await db.Table<Contacto>().CountAsync() == 0)
        {
            await db.InsertAllAsync(new[]
            {
                new Contacto { Nombre = "Ana García", Telefono = "11-4567-8901", Email = "ana.garcia@mail.com" },
                new Contacto { Nombre = "Bruno Pérez", Telefono = "11-2345-6789", Email = "bruno.perez@mail.com" },
                new Contacto { Nombre = "Carla López", Telefono = "11-9876-5432", Email = "carla.lopez@mail.com" },
            });
        }

        return db;
    }

    // ---------- CREATE ----------

    public async Task InsertarAsync(Contacto contacto)
    {
        Validar(contacto);
        var db = await _conexion.Value;
        await db.InsertAsync(contacto); // completa contacto.Id con el valor autoincremental
    }

    // ---------- READ ----------

    /// <summary>Paso 3b: todos los contactos, ordenados por nombre.</summary>
    public async Task<List<Contacto>> ObtenerTodosAsync()
    {
        var db = await _conexion.Value;
        return await db.Table<Contacto>().OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Contacto?> ObtenerPorIdAsync(int id)
    {
        var db = await _conexion.Value;
        return await db.FindAsync<Contacto>(id);
    }

    /// <summary>
    /// Paso 3c: búsqueda parcial por nombre, sin distinguir mayúsculas ni tildes
    /// ("perez" encuentra "Pérez"). Se filtra en C# porque el LIKE de SQLite sólo
    /// ignora mayúsculas en letras ASCII (no en Á, Ó, Ñ...).
    /// </summary>
    public async Task<List<Contacto>> BuscarPorNombreAsync(string texto)
    {
        var comparador = CultureInfo.InvariantCulture.CompareInfo;
        var opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        var todos = await ObtenerTodosAsync();
        return todos.Where(c => comparador.IndexOf(c.Nombre, texto, opciones) >= 0).ToList();
    }

    // ---------- UPDATE ----------

    public async Task ActualizarAsync(Contacto contacto)
    {
        Validar(contacto);
        var db = await _conexion.Value;
        await db.UpdateAsync(contacto);
    }

    // ---------- DELETE ----------

    public async Task EliminarAsync(int id)
    {
        var db = await _conexion.Value;
        await db.DeleteAsync<Contacto>(id);
    }

    /// <summary>
    /// SQLite no hace cumplir el largo de las columnas de texto (MaxLength sólo
    /// queda como metadato), así que las reglas se validan antes de guardar.
    /// </summary>
    private static void Validar(Contacto c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");
        if (c.Nombre.Length > MaxNombre)
            throw new ArgumentException($"El nombre no puede superar los {MaxNombre} caracteres.");
        if (!string.IsNullOrWhiteSpace(c.Email) && !c.Email.Contains('@'))
            throw new ArgumentException("El email no es válido.");
    }
}
