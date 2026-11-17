using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Tests.Servicios;

/// <summary>
/// La caché escribe archivos de verdad, así que cada prueba usa su propia
/// carpeta temporal y la borra al terminar (xUnit llama a Dispose después de
/// cada prueba). Ninguna prueba ve los archivos de otra.
/// </summary>
public class ServicioCacheTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "explorador-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    [Fact]
    public async Task Leer_DespuesDeGuardar_DevuelveElMismoContenidoYLaFecha()
    {
        var cache = new ServicioCache(_carpeta);
        var antes = DateTime.Now.AddSeconds(-1);

        await cache.GuardarAsync("character/?page=1", "{\"ok\":true}");
        var entrada = await cache.LeerAsync("character/?page=1");

        Assert.NotNull(entrada);
        Assert.Equal("{\"ok\":true}", entrada.Contenido);
        Assert.InRange(entrada.Fecha, antes, DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public async Task Leer_ClaveNuncaGuardada_DevuelveNull()
    {
        var cache = new ServicioCache(_carpeta);

        Assert.Null(await cache.LeerAsync("character/?page=99"));
    }

    [Fact]
    public async Task Guardar_ClavesConCaracteresNoValidosEnArchivos_NoSePisanEntreSi()
    {
        // Las claves son rutas con "/", "?", "&" y espacios: no pueden usarse
        // directamente como nombre de archivo.
        var cache = new ServicioCache(_carpeta);

        await cache.GuardarAsync("character/?page=1&name=rick sanchez", "A");
        await cache.GuardarAsync("character/?page=1&name=morty", "B");

        Assert.Equal("A", (await cache.LeerAsync("character/?page=1&name=rick sanchez"))!.Contenido);
        Assert.Equal("B", (await cache.LeerAsync("character/?page=1&name=morty"))!.Contenido);
    }

    [Fact]
    public async Task Guardar_MismaClave_ReemplazaLaCopiaAnterior()
    {
        var cache = new ServicioCache(_carpeta);

        await cache.GuardarAsync("character/1", "viejo");
        await cache.GuardarAsync("character/1", "nuevo");

        Assert.Equal("nuevo", (await cache.LeerAsync("character/1"))!.Contenido);
    }
}
