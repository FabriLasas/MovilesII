using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using Moq;

namespace ExploradorPersonajes.Tests.Integracion;

/// <summary>
/// Servicio de favoritos + repositorio SQLite real en memoria. Sólo la API
/// es un mock, porque es lo único externo. Crear un segundo servicio sobre
/// la misma base equivale a cerrar y volver a abrir la aplicación.
/// </summary>
public class ServicioFavoritosIntegracionTests : IAsyncLifetime
{
    private readonly BaseEnMemoria _base = new();
    private readonly Mock<IServicioPersonajes> _api = new();
    private readonly IMessenger _mensajero = new WeakReferenceMessenger();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _base.DisposeAsync();

    private ServicioFavoritos CrearServicio(FavoritosAnterioresFalsos? anteriores = null)
        => new(new RepositorioFavoritosSqlite(_base.Conexion), anteriores ?? new FavoritosAnterioresFalsos(), _api.Object, _mensajero);

    private void ApiDevuelve(params int[] ids)
        => _api.Setup(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(ResultadoApi<List<Personaje>>.Exito(ids.Select(id => DatosDePrueba.Personaje(id, $"Personaje {id}")).ToList()));

    [Fact]
    public async Task Alternar_MarcarYReabrirLaApp_ElFavoritoSigueGuardado()
    {
        var servicio = CrearServicio();
        await servicio.InicializarAsync();

        var resultado = await servicio.AlternarAsync(DatosDePrueba.Personaje(1, "Rick Sanchez"));

        var reabierto = CrearServicio();
        await reabierto.InicializarAsync();
        Assert.True(resultado);
        Assert.True(reabierto.EsFavorito(1));
        Assert.Equal("Rick Sanchez", Assert.Single(reabierto.ObtenerTodos()).Nombre);
    }

    [Fact]
    public async Task Alternar_DosVeces_LoQuitaTambienDeLaBase()
    {
        var servicio = CrearServicio();
        var personaje = DatosDePrueba.Personaje(1);

        await servicio.AlternarAsync(personaje);
        var resultado = await servicio.AlternarAsync(personaje);

        var reabierto = CrearServicio();
        await reabierto.InicializarAsync();
        Assert.False(resultado);
        Assert.False(servicio.EsFavorito(1));
        Assert.Empty(reabierto.ObtenerTodos());
    }

    [Fact]
    public async Task Alternar_AvisaElCambioALasDemasPantallas()
    {
        var recibidos = new List<FavoritoCambiadoMensaje>();
        _mensajero.Register<FavoritoCambiadoMensaje>(this, (_, m) => recibidos.Add(m));
        var servicio = CrearServicio();

        await servicio.AlternarAsync(DatosDePrueba.Personaje(4));

        var mensaje = Assert.Single(recibidos);
        Assert.Equal(4, mensaje.Personaje.Id);
        Assert.True(mensaje.EsFavorito);
    }

    [Fact]
    public async Task Inicializar_FavoritosDeLaVersionAnterior_PasanALaBaseEnElMismoOrden()
    {
        ApiDevuelve(5, 1);
        var anteriores = new FavoritosAnterioresFalsos(5, 1);
        var servicio = CrearServicio(anteriores);

        await servicio.InicializarAsync();

        Assert.Equal(new[] { 5, 1 }, servicio.ObtenerTodos().Select(p => p.Id));
        Assert.Equal(1, anteriores.VecesBorrado);
        var guardados = await new RepositorioFavoritosSqlite(_base.Conexion).ObtenerTodosAsync();
        Assert.Equal(new[] { 5, 1 }, guardados.Select(f => f.Id));
    }

    [Fact]
    public async Task Inicializar_MigracionSinConexion_ConservaLosIdsParaReintentar()
    {
        _api.Setup(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoApi<List<Personaje>>.Fallo(TipoError.SinConexion, "sin red"));
        var anteriores = new FavoritosAnterioresFalsos(5, 1);
        var servicio = CrearServicio(anteriores);

        await servicio.InicializarAsync();

        Assert.Empty(servicio.ObtenerTodos());
        Assert.Equal(0, anteriores.VecesBorrado);
        Assert.Equal(new[] { 5, 1 }, anteriores.LeerIds());
    }

    [Fact]
    public async Task Inicializar_SinFavoritosAnteriores_NoConsultaLaApi()
    {
        var servicio = CrearServicio();

        await servicio.InicializarAsync();

        _api.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Inicializar_VariasLlamadas_CargaUnaSolaVez()
    {
        ApiDevuelve(1);
        var servicio = CrearServicio(new FavoritosAnterioresFalsos(1));

        await Task.WhenAll(servicio.InicializarAsync(), servicio.InicializarAsync(), servicio.InicializarAsync());

        _api.Verify(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(servicio.ObtenerTodos());
    }

    [Fact]
    public async Task ActualizarDatos_ReemplazaLosDatosEnMemoriaYEnLaBase()
    {
        var servicio = CrearServicio();
        await servicio.AlternarAsync(DatosDePrueba.Personaje(1, "Rick"));

        await servicio.ActualizarDatosAsync(new[] { DatosDePrueba.Personaje(1, "Rick Sanchez") });

        var reabierto = CrearServicio();
        await reabierto.InicializarAsync();
        Assert.Equal("Rick Sanchez", servicio.ObtenerTodos()[0].Nombre);
        Assert.Equal("Rick Sanchez", reabierto.ObtenerTodos()[0].Nombre);
    }
}
