using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Tests.Dobles;

namespace ExploradorPersonajes.Tests.Integracion;

/// <summary>Repositorio de avistamientos contra SQLite real en memoria.</summary>
public class RepositorioAvistamientosSqliteTests : IAsyncLifetime
{
    private readonly BaseEnMemoria _base = new();
    private readonly RepositorioAvistamientosSqlite _repositorio;

    public RepositorioAvistamientosSqliteTests()
    {
        _repositorio = new RepositorioAvistamientosSqlite(_base.Conexion);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _base.DisposeAsync();

    private static Avistamiento Nuevo(int personajeId, DateTime fecha, string nota = "Nota de prueba") => new()
    {
        PersonajeId = personajeId,
        RutaFoto = $"/fotos/{Guid.NewGuid():N}.jpg",
        Nota = nota,
        Fecha = fecha
    };

    [Fact]
    public async Task Guardar_AsignaUnIdDistintoACadaAvistamiento()
    {
        var primero = Nuevo(1, DateTime.Now);
        var segundo = Nuevo(1, DateTime.Now);

        await _repositorio.GuardarAsync(primero);
        await _repositorio.GuardarAsync(segundo);

        Assert.True(primero.Id > 0);
        Assert.NotEqual(primero.Id, segundo.Id);
    }

    [Fact]
    public async Task Guardar_ConservaTodosLosDatosIncluidaLaUbicacion()
    {
        var avistamiento = Nuevo(1, new DateTime(2026, 11, 19, 15, 45, 0), "En la plaza");
        avistamiento.Latitud = -34.6037;
        avistamiento.Longitud = -58.3816;

        await _repositorio.GuardarAsync(avistamiento);
        var leido = Assert.Single(await _repositorio.ObtenerPorPersonajeAsync(1));

        Assert.Equal("En la plaza", leido.Nota);
        Assert.Equal(avistamiento.RutaFoto, leido.RutaFoto);
        Assert.Equal(-34.6037, leido.Latitud);
        Assert.Equal(-58.3816, leido.Longitud);
        Assert.Equal(new DateTime(2026, 11, 19, 15, 45, 0), leido.Fecha);
        Assert.Equal("-34.60370, -58.38160", leido.TextoUbicacion);
        Assert.Equal("19/11/2026 15:45", leido.TextoFecha);
    }

    [Fact]
    public async Task Guardar_SinUbicacion_LaLeeComoVacia()
    {
        await _repositorio.GuardarAsync(Nuevo(1, DateTime.Now));

        var leido = Assert.Single(await _repositorio.ObtenerPorPersonajeAsync(1));

        Assert.Null(leido.Latitud);
        Assert.False(leido.TieneUbicacion);
        Assert.Equal("Sin ubicación", leido.TextoUbicacion);
    }

    [Fact]
    public async Task ObtenerPorPersonaje_DevuelveSoloLosDeEsePersonajeDelMasRecienteAlMasViejo()
    {
        var hoy = new DateTime(2026, 11, 19);
        await _repositorio.GuardarAsync(Nuevo(1, hoy.AddDays(-2), "viejo"));
        await _repositorio.GuardarAsync(Nuevo(2, hoy, "de otro personaje"));
        await _repositorio.GuardarAsync(Nuevo(1, hoy, "nuevo"));
        await _repositorio.GuardarAsync(Nuevo(1, hoy.AddDays(-1), "medio"));

        var lista = await _repositorio.ObtenerPorPersonajeAsync(1);

        Assert.Equal(new[] { "nuevo", "medio", "viejo" }, lista.Select(a => a.Nota));
    }

    [Fact]
    public async Task ObtenerPorPersonaje_MismaFecha_ElUltimoGuardadoVaPrimero()
    {
        var fecha = new DateTime(2026, 11, 19, 12, 0, 0);
        await _repositorio.GuardarAsync(Nuevo(1, fecha, "primero"));
        await _repositorio.GuardarAsync(Nuevo(1, fecha, "segundo"));

        var lista = await _repositorio.ObtenerPorPersonajeAsync(1);

        Assert.Equal(new[] { "segundo", "primero" }, lista.Select(a => a.Nota));
    }

    [Fact]
    public async Task Eliminar_QuitaSoloEseAvistamiento()
    {
        var uno = Nuevo(1, DateTime.Now, "uno");
        var dos = Nuevo(1, DateTime.Now, "dos");
        await _repositorio.GuardarAsync(uno);
        await _repositorio.GuardarAsync(dos);

        await _repositorio.EliminarAsync(uno.Id);

        Assert.Equal("dos", Assert.Single(await _repositorio.ObtenerPorPersonajeAsync(1)).Nota);
    }

    [Fact]
    public async Task ConvivenConLosFavoritosEnLaMismaBase()
    {
        // En la app, ambos repositorios comparten la conexión y el archivo.
        var favoritos = new RepositorioFavoritosSqlite(_base.Conexion);

        await favoritos.AgregarAsync(DatosDePrueba.Personaje(1));
        await _repositorio.GuardarAsync(Nuevo(1, DateTime.Now));

        Assert.Single(await favoritos.ObtenerTodosAsync());
        Assert.Single(await _repositorio.ObtenerPorPersonajeAsync(1));
    }
}
