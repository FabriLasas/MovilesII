using System.Globalization;
using AgendaContactos.Models;
using SQLite;

namespace AgendaContactos.Data;

// Encapsula el acceso a SQLite y las operaciones CRUD sobre Contacto.
public class AgendaDatabase
{
    public const int MaxNombre = 50;

    private readonly SQLiteConnection _db;

    public AgendaDatabase(string rutaDb)
    {
        // Paso 2: crea el archivo .db (si no existe) y la tabla a partir de los atributos.
        _db = new SQLiteConnection(rutaDb);
        _db.CreateTable<Contacto>();
    }

    // CREATE
    public int Insertar(Contacto c)
    {
        Validar(c);
        return _db.Insert(c); // sqlite-net completa c.Id con el valor autoincremental
    }

    // READ
    public List<Contacto> ObtenerTodos() =>
        _db.Table<Contacto>().OrderBy(c => c.Nombre).ToList();

    public Contacto? ObtenerPorId(int id) =>
        _db.Find<Contacto>(id);

    // Búsqueda parcial por nombre, sin distinguir mayúsculas ni tildes ("perez" encuentra "Pérez").
    // Se filtra en C# porque el LIKE de SQLite sólo ignora mayúsculas en letras ASCII (no en Á, Ó, Ñ...).
    public List<Contacto> BuscarPorNombre(string texto)
    {
        var comparador = CultureInfo.InvariantCulture.CompareInfo;
        var opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        return ObtenerTodos()
            .Where(c => comparador.IndexOf(c.Nombre, texto, opciones) >= 0)
            .ToList();
    }

    // UPDATE
    public int Actualizar(Contacto c)
    {
        Validar(c);
        return _db.Update(c);
    }

    // DELETE
    public int Eliminar(int id) =>
        _db.Delete<Contacto>(id);

    public int Cantidad() => _db.Table<Contacto>().Count();

    // SQLite no hace cumplir el largo de las columnas de texto, así que lo validamos acá.
    private static void Validar(Contacto c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");
        if (c.Nombre.Length > MaxNombre)
            throw new ArgumentException($"El nombre no puede superar los {MaxNombre} caracteres.");
    }
}
