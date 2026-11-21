using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using ExploradorPersonajes.ViewModels;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

/// <summary>
/// Fallas que no deberían pasar nunca (la base local no responde, un error
/// del sistema) pero que, si pasan, tienen que terminar en un mensaje y no en
/// la aplicación cerrada.
/// </summary>
public class ManejoDeExcepcionesTests
{
    private readonly ServiciosSimulados _s = new();
    private static readonly InvalidOperationException FallaDeLaBase = new("base bloqueada");

    [Fact]
    public async Task ServicioPersonajes_ExcepcionNoPrevista_SeConvierteEnErrorInesperado()
    {
        var conectividad = new Mock<IServicioConectividad>();
        var cache = new Mock<IServicioCache>();
        var servicio = new ServicioPersonajes(
            new HttpClient(ManejadorHttpFalso.QueLance(new InvalidOperationException("falla rara"))),
            conectividad.Object,
            cache.Object);

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.Inesperado, resultado.Error);
        Assert.Equal("Ocurrió un error inesperado. Intentá de nuevo. (falla rara)", resultado.Mensaje);
    }

    [Fact]
    public async Task Lista_SiLaBaseDeFavoritosFalla_IgualCargaLosPersonajes()
    {
        _s.Favoritos.Setup(f => f.InicializarAsync()).ThrowsAsync(FallaDeLaBase);
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(826, true, 1, 2));
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Personajes.Count);
        Assert.False(vm.HayError);
    }

    [Fact]
    public void PestanaFavoritos_SiLaBaseFalla_MuestraElErrorEnLugarDeCerrarse()
    {
        _s.Favoritos.Setup(f => f.InicializarAsync()).ThrowsAsync(FallaDeLaBase);

        var vm = _s.CrearFavoritosViewModel();

        Assert.True(vm.HayError);
        Assert.Equal("No se pudieron leer los favoritos guardados. (base bloqueada)", vm.MensajeEstado);
    }

    [Fact]
    public void PestanaFavoritos_SiFallaGuardarLosDatosNuevos_SigueMostrandoLosGuardados()
    {
        _s.Favoritos.Setup(f => f.ObtenerTodos()).Returns(new[] { DatosDePrueba.Personaje(1, "Guardado") });
        _s.Api.Setup(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(ResultadoApi<List<Personaje>>.Exito(new List<Personaje> { DatosDePrueba.Personaje(1, "Nuevo") }));
        _s.Favoritos.Setup(f => f.ActualizarDatosAsync(It.IsAny<IEnumerable<Personaje>>())).ThrowsAsync(FallaDeLaBase);

        var vm = _s.CrearFavoritosViewModel();

        Assert.Equal("Guardado", Assert.Single(vm.Favoritos).Nombre);
        Assert.False(vm.HayError);
    }

    [Fact]
    public async Task Detalle_SiNoSePuedeGuardarElFavorito_LoInformaYNoVibra()
    {
        var personaje = DatosDePrueba.Personaje(1);
        _s.Favoritos.Setup(f => f.AlternarAsync(personaje)).ThrowsAsync(FallaDeLaBase);
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(new Dictionary<string, object> { [DetalleViewModel.ClaveParametro] = personaje });

        await vm.AlternarFavoritoCommand.ExecuteAsync(null);

        Assert.False(vm.EsFavorito);
        Assert.True(vm.HayError);
        Assert.Equal("No se pudo guardar el favorito. (base bloqueada)", vm.MensajeEstado);
        _s.Haptica.Verify(h => h.ConfirmarToque(), Times.Never);
    }

    [Fact]
    public async Task Tarjeta_SiNoSePuedeGuardarElFavorito_LaEstrellaQuedaComoEstaba()
    {
        var personaje = DatosDePrueba.Personaje(1);
        _s.Favoritos.Setup(f => f.AlternarAsync(personaje)).ThrowsAsync(FallaDeLaBase);
        var item = new PersonajeItemViewModel(personaje, _s.DeItem());

        await item.AlternarFavoritoCommand.ExecuteAsync(null);

        Assert.False(item.EsFavorito);
        _s.Haptica.Verify(h => h.ConfirmarToque(), Times.Never);
    }
}
