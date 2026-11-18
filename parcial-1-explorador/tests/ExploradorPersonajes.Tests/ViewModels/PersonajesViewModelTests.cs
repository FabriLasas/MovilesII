using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

/// <summary>
/// Pruebas de la pantalla principal sin interfaz gráfica: se ejecutan los
/// comandos como lo haría un botón y se verifica el estado que vería la vista
/// (lista, mensaje, color de error). La API se reemplaza por un mock.
/// </summary>
public class PersonajesViewModelTests
{
    private readonly ServiciosSimulados _s = new();

    [Fact]
    public async Task CargarPersonajes_Exito_LlenaLaListaEInformaElTotal()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(826, true, 1, 2));
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Assert.Equal(new[] { 1, 2 }, vm.Personajes.Select(p => p.Id));
        Assert.Equal("Mostrando 2 de 826 personajes.", vm.MensajeEstado);
        Assert.False(vm.HayError);
        Assert.False(vm.EstaCargando);
        Assert.False(vm.MostrandoCache);
    }

    [Fact]
    public async Task CargarPersonajes_CargaLosFavoritosAntesDeArmarLasTarjetas()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(1, false, 1));
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        _s.Favoritos.Verify(f => f.InicializarAsync(), Times.Once);
    }

    [Fact]
    public async Task CargarPersonajes_ErrorDelServidor_MuestraElMensajeComoError()
    {
        _s.ResponderPagina(1, ResultadoApi<RespuestaPaginada>.Fallo(TipoError.ErrorServidor, "El servidor tuvo un problema interno. (Error 500)", 500));
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Assert.Empty(vm.Personajes);
        Assert.True(vm.HayError);
        Assert.Equal("El servidor tuvo un problema interno. (Error 500)", vm.MensajeEstado);
        Assert.False(vm.EstaCargando);
    }

    [Fact]
    public async Task CargarPersonajes_BusquedaSinResultados_SeInformaSinFormatoDeError()
    {
        _s.ResponderPagina(1, ResultadoApi<RespuestaPaginada>.Fallo(TipoError.SinResultados, "No se encontraron personajes para esa búsqueda.", 404));
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Assert.False(vm.HayError);
        Assert.Equal("No se encontraron personajes para esa búsqueda.", vm.MensajeEstado);
    }

    [Fact]
    public async Task CargarPersonajes_DatosDesdeLaCache_MuestraLaFranjaDeAviso()
    {
        var pagina = DatosDePrueba.Pagina(826, true, 1);
        _s.ResponderPagina(1, ResultadoApi<RespuestaPaginada>.ExitoDesdeCache(
            pagina, new DateTime(2026, 11, 10, 18, 30, 0), TipoError.SinConexion, "sin red"));
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        Assert.True(vm.MostrandoCache);
        Assert.Equal("Sin conexión · Mostrando datos guardados el 10/11 a las 18:30", vm.AvisoCache);
        Assert.Equal("Mostrando 1 de 826 personajes (copia guardada).", vm.MensajeEstado);
    }

    [Fact]
    public async Task CargarPersonajes_EnviaElTextoYElFiltroElegidos()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(1, false, 1));
        var vm = _s.CrearPersonajesViewModel();
        vm.FiltroSeleccionado = vm.OpcionesFiltro.Single(o => o.Etiqueta == "Muertos");
        vm.TextoBusqueda = "rick";

        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        _s.Api.Verify(a => a.ObtenerPersonajesAsync(1, "rick", "dead", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Buscar_RespuestaViejaQueLlegaTarde_SeDescarta()
    {
        // Dos consultas en vuelo: la primera responde DESPUÉS que la segunda.
        // Si se mostrara, pisaría los resultados de la consulta más reciente.
        var primera = new TaskCompletionSource<ResultadoApi<RespuestaPaginada>>();
        var segunda = new TaskCompletionSource<ResultadoApi<RespuestaPaginada>>();
        _s.Api.SetupSequence(a => a.ObtenerPersonajesAsync(1, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .Returns(primera.Task)
              .Returns(segunda.Task);
        var vm = _s.CrearPersonajesViewModel();

        var cargaVieja = vm.CargarPersonajesCommand.ExecuteAsync(null);
        var cargaNueva = vm.RefrescarCommand.ExecuteAsync(null);
        segunda.SetResult(DatosDePrueba.PaginaExitosa(1, false, 2));
        await cargaNueva;
        primera.SetResult(DatosDePrueba.PaginaExitosa(1, false, 1));
        await cargaVieja;

        Assert.Equal(2, Assert.Single(vm.Personajes).Id);
    }

    [Fact]
    public async Task CargarMas_HayPaginaSiguiente_AgregaAlFinal()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(4, true, 1, 2));
        _s.ResponderPagina(2, DatosDePrueba.PaginaExitosa(4, false, 3, 4));
        var vm = _s.CrearPersonajesViewModel();
        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        await vm.CargarMasCommand.ExecuteAsync(null);

        Assert.Equal(new[] { 1, 2, 3, 4 }, vm.Personajes.Select(p => p.Id));
        Assert.Equal("Mostrando 4 de 4 personajes.", vm.MensajeEstado);
    }

    [Fact]
    public async Task CargarMas_UltimaPagina_NoPideNada()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(2, false, 1, 2));
        var vm = _s.CrearPersonajesViewModel();
        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        await vm.CargarMasCommand.ExecuteAsync(null);

        _s.Api.Verify(a => a.ObtenerPersonajesAsync(2, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CargarMas_SinCargaPrevia_NoPideNada()
    {
        var vm = _s.CrearPersonajesViewModel();

        await vm.CargarMasCommand.ExecuteAsync(null);

        _s.Api.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TextoBusqueda_EscrituraRapida_HaceUnaSolaConsultaConElTextoFinal()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(1, false, 1));
        var vm = _s.CrearPersonajesViewModel();

        vm.TextoBusqueda = "r";
        vm.TextoBusqueda = "ri";
        vm.TextoBusqueda = "rick";
        await Task.Delay(1500);

        _s.Api.Verify(a => a.ObtenerPersonajesAsync(1, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _s.Api.Verify(a => a.ObtenerPersonajesAsync(1, "rick", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlternarSimulacion_ActivaElModoSinRedYCambiaElTexto()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(1, false, 1));
        var vm = _s.CrearPersonajesViewModel();

        await vm.AlternarSimulacionCommand.ExecuteAsync(null);

        _s.Conectividad.VerifySet(c => c.SimularSinConexion = true);
        Assert.Equal("Restaurar red", vm.TextoSimulacion);
    }

    [Fact]
    public async Task ProbarError404_PideUnIdInexistenteYMuestraElError()
    {
        _s.Api.Setup(a => a.ObtenerPersonajePorIdAsync(9999, It.IsAny<CancellationToken>()))
              .ReturnsAsync(ResultadoApi<Personaje>.Fallo(TipoError.NoEncontrado, "El personaje solicitado no existe. (Error 404)", 404));
        var vm = _s.CrearPersonajesViewModel();

        await vm.ProbarError404Command.ExecuteAsync(null);

        Assert.True(vm.HayError);
        Assert.Equal("El personaje solicitado no existe. (Error 404)", vm.MensajeEstado);
    }

    [Fact]
    public async Task MensajeDeFavorito_DesdeOtraPantalla_ActualizaLaEstrellaDeLaTarjeta()
    {
        _s.ResponderPagina(1, DatosDePrueba.PaginaExitosa(2, false, 1, 2));
        var vm = _s.CrearPersonajesViewModel();
        await vm.CargarPersonajesCommand.ExecuteAsync(null);

        _s.Mensajero.Send(new FavoritoCambiadoMensaje(DatosDePrueba.Personaje(2), true));

        Assert.False(vm.Personajes[0].EsFavorito);
        Assert.True(vm.Personajes[1].EsFavorito);
        Assert.Equal("★", vm.Personajes[1].IconoFavorito);
    }
}
