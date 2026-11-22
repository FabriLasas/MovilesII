using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Tests.Dobles;

namespace ExploradorPersonajes.Tests.Integracion;

/// <summary>
/// Pruebas de integración del repositorio contra SQLite real en memoria. A
/// diferencia de las unitarias, acá no hay mocks: se verifica que las
/// consultas, la clave primaria y las transacciones funcionen de verdad.
/// </summary>
public class RepositorioFavoritosSqliteTests : IAsyncLifetime
{
    private static readonly DateTime FechaFija = new(2026, 11, 18, 10, 30, 0);

    private readonly BaseEnMemoria _base = new();
    private readonly RepositorioFavoritosSqlite _repositorio;

    public RepositorioFavoritosSqliteTests()
    {
        _repositorio = new RepositorioFavoritosSqlite(_base.Conexion, () => FechaFija);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _base.DisposeAsync();

    [Fact]
    public async Task ObtenerTodos_BaseNueva_DevuelveListaVacia()
    {
        Assert.Empty(await _repositorio.ObtenerTodosAsync());
    }

    [Fact]
    public async Task Agregar_VariosPersonajes_LosDevuelveEnElOrdenEnQueSeAgregaron()
    {
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(3));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(2));

        var todos = await _repositorio.ObtenerTodosAsync();

        Assert.Equal(new[] { 3, 1, 2 }, todos.Select(f => f.Id));
    }

    [Fact]
    public async Task Agregar_GuardaLosDatosDelPersonajeYLaFechaDeAlta()
    {
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1, "Rick Sanchez", "Dead"));

        var guardado = Assert.Single(await _repositorio.ObtenerTodosAsync());
        Assert.Equal("Rick Sanchez", guardado.Nombre);
        Assert.Equal("Dead", guardado.Estado);
        Assert.Equal("Human", guardado.Especie);
        Assert.Equal("Earth (C-137)", guardado.Origen);
        Assert.Equal("Citadel of Ricks", guardado.UbicacionActual);
        Assert.Equal(FechaFija, guardado.FechaAlta);

        // Y se puede volver a convertir en el modelo que usan las pantallas.
        var personaje = guardado.APersonaje();
        Assert.Equal("Muerto", personaje.EstadoTraducido);
        Assert.Equal("Earth (C-137)", personaje.Origen!.Nombre);
    }

    [Fact]
    public async Task Agregar_PersonajeRepetido_NoLoDuplicaNiCambiaSuPosicion()
    {
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(2));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));

        var todos = await _repositorio.ObtenerTodosAsync();

        Assert.Equal(new[] { 1, 2 }, todos.Select(f => f.Id));
    }

    [Fact]
    public async Task Quitar_EliminaSoloEseFavorito()
    {
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(2));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(3));

        await _repositorio.QuitarAsync(2);

        Assert.Equal(new[] { 1, 3 }, (await _repositorio.ObtenerTodosAsync()).Select(f => f.Id));
    }

    [Fact]
    public async Task Quitar_IdQueNoEsta_NoFalla()
    {
        await _repositorio.QuitarAsync(9999);

        Assert.Empty(await _repositorio.ObtenerTodosAsync());
    }

    [Fact]
    public async Task Agregar_DespuesDeQuitar_VuelveAlFinalDeLaLista()
    {
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(2));
        await _repositorio.QuitarAsync(1);
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));

        Assert.Equal(new[] { 2, 1 }, (await _repositorio.ObtenerTodosAsync()).Select(f => f.Id));
    }

    [Fact]
    public async Task ActualizarDatos_CambiaLosDatosPeroConservaOrdenYFecha()
    {
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1, "Rick"));
        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(2, "Morty"));
        var actualizado = DatosDePrueba.Personaje(1, "Rick Sanchez", "Dead");
        var repositorioMasTarde = new RepositorioFavoritosSqlite(_base.Conexion, () => FechaFija.AddDays(5));

        await repositorioMasTarde.ActualizarDatosAsync(new[] { actualizado });

        var todos = await _repositorio.ObtenerTodosAsync();
        Assert.Equal(new[] { 1, 2 }, todos.Select(f => f.Id));
        Assert.Equal("Rick Sanchez", todos[0].Nombre);
        Assert.Equal("Dead", todos[0].Estado);
        Assert.Equal(FechaFija, todos[0].FechaAlta);
    }

    [Fact]
    public async Task ActualizarDatos_PersonajeQueNoEsFavorito_NoLoAgrega()
    {
        await _repositorio.ActualizarDatosAsync(new[] { DatosDePrueba.Personaje(7) });

        Assert.Empty(await _repositorio.ObtenerTodosAsync());
    }

    [Fact]
    public async Task DosBasesEnMemoria_NoCompartenDatos()
    {
        await using var otra = new BaseEnMemoriaAsync();
        var otroRepositorio = new RepositorioFavoritosSqlite(otra.Base.Conexion);

        await _repositorio.AgregarAsync(DatosDePrueba.Personaje(1));

        Assert.Empty(await otroRepositorio.ObtenerTodosAsync());
    }

    /// <summary>Envoltorio para usar la base con "await using" dentro de una prueba.</summary>
    private sealed class BaseEnMemoriaAsync : IAsyncDisposable
    {
        public BaseEnMemoria Base { get; } = new();

        public async ValueTask DisposeAsync() => await Base.DisposeAsync();
    }
}
