using System.Security.Cryptography;
using System.Text;

namespace ExploradorPersonajes.Servicios;

/// <summary>Contenido guardado y el momento en que se guardó.</summary>
public record EntradaCache(string Contenido, DateTime Fecha);

/// <summary>
/// Copia local de las últimas respuestas exitosas de la API, usada como
/// respaldo cuando no hay red.
/// </summary>
public interface IServicioCache
{
    Task GuardarAsync(string clave, string contenido);

    Task<EntradaCache?> LeerAsync(string clave);
}

/// <summary>
/// Guarda cada respuesta como un archivo JSON en la carpeta de datos de la
/// aplicación. Se eligieron archivos en lugar de Preferences porque
/// Preferences está pensado para valores chicos (ajustes, banderas) y una
/// página de personajes ocupa varios kilobytes.
/// </summary>
public class ServicioCache : IServicioCache
{
    private readonly string _carpeta;

    public ServicioCache(IFileSystem sistemaDeArchivos)
    {
        _carpeta = Path.Combine(sistemaDeArchivos.AppDataDirectory, "cache");
        Directory.CreateDirectory(_carpeta);
    }

    public async Task GuardarAsync(string clave, string contenido)
    {
        try
        {
            await File.WriteAllTextAsync(RutaDe(clave), contenido);
        }
        catch (IOException)
        {
            // La caché es un extra: si no se puede escribir (disco lleno,
            // archivo en uso) la aplicación sigue funcionando igual.
        }
    }

    public async Task<EntradaCache?> LeerAsync(string clave)
    {
        var ruta = RutaDe(clave);
        if (!File.Exists(ruta)) return null;

        try
        {
            var contenido = await File.ReadAllTextAsync(ruta);
            return new EntradaCache(contenido, File.GetLastWriteTime(ruta));
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// La clave es la ruta de la consulta (por ejemplo "character/?page=2"),
    /// que contiene caracteres no válidos en un nombre de archivo. Se usa su
    /// hash como nombre: es único por consulta y siempre válido.
    /// </summary>
    private string RutaDe(string clave)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clave)));
        return Path.Combine(_carpeta, $"{hash}.json");
    }
}
