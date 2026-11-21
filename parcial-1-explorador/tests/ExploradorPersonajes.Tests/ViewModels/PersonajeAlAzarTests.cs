using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using ExploradorPersonajes.ViewModels;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

/// <summary>
/// "Agitá el teléfono para un personaje al azar". El acelerómetro se simula
/// disparando su evento con Moq, y el azar se fija para saber qué id esperar.
/// </summary>
public class PersonajeAlAzarTests
{
    private readonly ServiciosSimulados _s = new();

    private void ApiDevuelvePersonaje(int id, string nombre)
        => _s.Api.Setup(a => a.ObtenerPersonajePorIdAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(ResultadoApi<Personaje>.Exito(DatosDePrueba.Personaje(id, nombre)));

    private void Agitar() => _s.Movimiento.Raise(m => m.Agitado += null, EventArgs.Empty);

    /// <summary>Crea el ViewModel con la lista a la vista, como al abrir la app.</summary>
    private PersonajesViewModel ListaVisible()
    {
        var vm = _s.CrearPersonajesViewModel();
        vm.ActivarSensorCommand.Execute(null);
        return vm;
    }

    [Fact]
    public void Agitar_AbreElDetalleDeUnPersonajeAlAzarYVibra()
    {
        _s.IdAlAzar = 7;
        ApiDevuelvePersonaje(7, "Abradolf Lincler");
        var vm = ListaVisible();

        Agitar();

        _s.Navegacion.Verify(n => n.IrADetalleAsync(It.Is<Personaje>(p => p.Id == 7)), Times.Once);
        _s.Haptica.Verify(h => h.Vibrar(It.IsAny<TimeSpan>()), Times.Once);
        Assert.Equal("Al azar: Abradolf Lincler.", vm.MensajeEstado);
    }

    [Fact]
    public void Agitar_AntesDeCargarNada_EligeEntreLos826DelCatalogo()
    {
        ApiDevuelvePersonaje(42, "X");
        ListaVisible();

        Agitar();

        Assert.Equal(826, _s.TotalPedidoAlAzar);
    }

    [Fact]
    public async Task Agitar_DespuesDeCargarElListado_UsaElTotalRealDelCatalogo()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(900, true, 1));
        ApiDevuelvePersonaje(42, "X");
        var vm = ListaVisible();
        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Agitar();

        Assert.Equal(900, _s.TotalPedidoAlAzar);
    }

    [Fact]
    public async Task Agitar_ConUnFiltroActivo_NoTomaElTotalFiltradoComoCatalogo()
    {
        // 287 son los muertos, no el catálogo: elegir entre 1 y 287 dejaría
        // afuera a más de la mitad de los personajes.
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(287, true, 1));
        ApiDevuelvePersonaje(42, "X");
        var vm = ListaVisible();
        vm.FiltroSeleccionado = vm.OpcionesFiltro.Single(o => o.Etiqueta == "Muertos");
        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Agitar();

        Assert.Equal(826, _s.TotalPedidoAlAzar);
    }

    [Fact]
    public void Agitar_VariasVecesSeguidas_AbreUnSoloPersonaje()
    {
        // La respuesta de la API queda pendiente: mientras tanto llegan más
        // avisos del sensor, que deben ignorarse.
        var respuesta = new TaskCompletionSource<ResultadoApi<Personaje>>();
        _s.Api.Setup(a => a.ObtenerPersonajePorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .Returns(respuesta.Task);
        ListaVisible();

        Agitar();
        Agitar();
        Agitar();
        respuesta.SetResult(ResultadoApi<Personaje>.Exito(DatosDePrueba.Personaje(42)));

        _s.Api.Verify(a => a.ObtenerPersonajePorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _s.Navegacion.Verify(n => n.IrADetalleAsync(It.IsAny<Personaje>()), Times.Once);
    }

    [Fact]
    public void Agitar_SinConexion_MuestraElErrorYNoNavega()
    {
        _s.Api.Setup(a => a.ObtenerPersonajePorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(ResultadoApi<Personaje>.Fallo(TipoError.SinConexion, "No hay conexión a internet."));
        var vm = ListaVisible();

        Agitar();

        Assert.True(vm.HayError);
        Assert.Equal("No hay conexión a internet.", vm.MensajeEstado);
        _s.Navegacion.Verify(n => n.IrADetalleAsync(It.IsAny<Personaje>()), Times.Never);
    }

    [Fact]
    public void Agitar_ConLaListaFueraDeVista_NoAbreNada()
    {
        // Un aviso del sensor que llega después de salir de la lista (quedó
        // encolado al abrir un detalle) no debe abrir otro personaje.
        ApiDevuelvePersonaje(42, "X");
        var vm = ListaVisible();
        vm.DesactivarSensorCommand.Execute(null);

        Agitar();

        _s.Api.Verify(a => a.ObtenerPersonajePorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _s.Navegacion.Verify(n => n.IrADetalleAsync(It.IsAny<Personaje>()), Times.Never);
    }

    [Fact]
    public void Agitar_AntesDeQueLaListaAparezca_NoAbreNada()
    {
        ApiDevuelvePersonaje(42, "X");
        _s.CrearPersonajesViewModel();

        Agitar();

        _s.Navegacion.Verify(n => n.IrADetalleAsync(It.IsAny<Personaje>()), Times.Never);
    }

    [Fact]
    public async Task BotonAlAzar_HaceLoMismoQueAgitar()
    {
        ApiDevuelvePersonaje(42, "Bird Person");
        var vm = ListaVisible();

        await vm.PersonajeAlAzarCommand.ExecuteAsync(null);

        _s.Navegacion.Verify(n => n.IrADetalleAsync(It.Is<Personaje>(p => p.Nombre == "Bird Person")), Times.Once);
    }

    [Fact]
    public void ActivarYDesactivarSensor_EnciendenYApaganElAcelerometro()
    {
        var vm = _s.CrearPersonajesViewModel();

        vm.ActivarSensorCommand.Execute(null);
        vm.DesactivarSensorCommand.Execute(null);

        _s.Movimiento.Verify(m => m.Iniciar(), Times.Once);
        _s.Movimiento.Verify(m => m.Detener(), Times.Once);
    }

    [Theory]
    [InlineData(true, "Tocá «Cargar datos», escribí un nombre o agitá el teléfono para un personaje al azar.")]
    [InlineData(false, "Tocá «Cargar datos» o escribí un nombre para buscar.")]
    public void MensajeInicial_SoloMencionaAgitarSiHayAcelerometro(bool hayAcelerometro, string esperado)
    {
        _s.Movimiento.SetupGet(m => m.EstaDisponible).Returns(hayAcelerometro);

        var vm = ListaVisible();

        Assert.Equal(hayAcelerometro, vm.SensorDisponible);
        Assert.Equal(esperado, vm.MensajeEstado);
    }
}
