using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Tests.Dobles;
using ExploradorPersonajes.ViewModels;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

public class DetalleViewModelTests
{
    private readonly ServiciosSimulados _s = new();

    private static Dictionary<string, object> Parametros(object valor)
        => new() { [DetalleViewModel.ClaveParametro] = valor };

    [Fact]
    public void Recibir_ConPersonaje_MuestraSusDatosYSiEsFavorito()
    {
        _s.Favoritos.Setup(f => f.EsFavorito(1)).Returns(true);
        var vm = _s.CrearDetalleViewModel();

        vm.Recibir(Parametros(DatosDePrueba.Personaje(1, "Rick Sanchez")));

        Assert.Equal("Rick Sanchez", vm.Personaje!.Nombre);
        Assert.Equal("Rick Sanchez", vm.Titulo);
        Assert.True(vm.EsFavorito);
        Assert.Equal("★  Quitar de favoritos", vm.TextoBotonFavorito);
        Assert.False(vm.HayError);
    }

    [Fact]
    public void Recibir_SinParametro_InformaElErrorEnPantalla()
    {
        var vm = _s.CrearDetalleViewModel();

        vm.Recibir(new Dictionary<string, object>());

        Assert.Null(vm.Personaje);
        Assert.True(vm.HayError);
        Assert.Equal("No se recibió el personaje a mostrar.", vm.MensajeEstado);
    }

    [Fact]
    public void Recibir_ParametroDeOtroTipo_InformaElErrorEnPantalla()
    {
        var vm = _s.CrearDetalleViewModel();

        vm.Recibir(Parametros("1"));

        Assert.Null(vm.Personaje);
        Assert.True(vm.HayError);
    }

    [Fact]
    public async Task AlternarFavorito_MarcaElPersonajeYVibra()
    {
        var personaje = DatosDePrueba.Personaje(1);
        _s.Favoritos.Setup(f => f.AlternarAsync(personaje)).ReturnsAsync(true);
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(Parametros(personaje));

        await vm.AlternarFavoritoCommand.ExecuteAsync(null);

        Assert.True(vm.EsFavorito);
        Assert.Equal("★  Quitar de favoritos", vm.TextoBotonFavorito);
        _s.Haptica.Verify(h => h.ConfirmarToque(), Times.Once);
    }

    [Fact]
    public async Task AlternarFavorito_SinPersonaje_NoHaceNada()
    {
        var vm = _s.CrearDetalleViewModel();

        await vm.AlternarFavoritoCommand.ExecuteAsync(null);

        _s.Favoritos.Verify(f => f.AlternarAsync(It.IsAny<Personaje>()), Times.Never);
        _s.Haptica.Verify(h => h.ConfirmarToque(), Times.Never);
    }

    // --- Avistamientos ---

    private static Avistamiento Avistamiento(int id, int personajeId, string nota) => new()
    {
        Id = id,
        PersonajeId = personajeId,
        Nota = nota,
        RutaFoto = $"/fotos/{id}.jpg",
        Fecha = new DateTime(2026, 11, 20)
    };

    [Fact]
    public void Recibir_CargaLosAvistamientosDelPersonaje()
    {
        _s.Avistamientos.Setup(a => a.ObtenerPorPersonajeAsync(1))
            .ReturnsAsync(new List<Avistamiento> { Avistamiento(2, 1, "segundo"), Avistamiento(1, 1, "primero") });
        var vm = _s.CrearDetalleViewModel();

        vm.Recibir(Parametros(DatosDePrueba.Personaje(1)));

        Assert.Equal(new[] { "segundo", "primero" }, vm.Avistamientos.Select(a => a.Nota));
        Assert.Equal("2 avistamientos", vm.ResumenAvistamientos);
    }

    [Fact]
    public void Recibir_SinAvistamientos_InvitaARegistrarElPrimero()
    {
        var vm = _s.CrearDetalleViewModel();

        vm.Recibir(Parametros(DatosDePrueba.Personaje(1)));

        Assert.Empty(vm.Avistamientos);
        Assert.Equal("Todavía no lo avistaste. Registrá el primero con una foto.", vm.ResumenAvistamientos);
    }

    [Fact]
    public async Task NuevoAvistamiento_NavegaConElPersonaje()
    {
        var personaje = DatosDePrueba.Personaje(3);
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(Parametros(personaje));

        await vm.NuevoAvistamientoCommand.ExecuteAsync(null);

        _s.Navegacion.Verify(n => n.IrANuevoAvistamientoAsync(personaje), Times.Once);
    }

    [Fact]
    public void AvistamientoGuardado_DelMismoPersonaje_SeAgregaArriba()
    {
        _s.Avistamientos.Setup(a => a.ObtenerPorPersonajeAsync(1))
            .ReturnsAsync(new List<Avistamiento> { Avistamiento(1, 1, "viejo") });
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(Parametros(DatosDePrueba.Personaje(1)));

        _s.Mensajero.Send(new AvistamientoGuardadoMensaje(Avistamiento(2, 1, "nuevo")));

        Assert.Equal(new[] { "nuevo", "viejo" }, vm.Avistamientos.Select(a => a.Nota));
        Assert.Equal("2 avistamientos", vm.ResumenAvistamientos);
    }

    [Fact]
    public void AvistamientoGuardado_DeOtroPersonaje_SeIgnora()
    {
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(Parametros(DatosDePrueba.Personaje(1)));

        _s.Mensajero.Send(new AvistamientoGuardadoMensaje(Avistamiento(9, 2, "de otro")));

        Assert.Empty(vm.Avistamientos);
    }

    [Fact]
    public async Task EliminarAvistamiento_LoBorraDeLaBaseYSuFotoDelDisco()
    {
        _s.Avistamientos.Setup(a => a.ObtenerPorPersonajeAsync(1))
            .ReturnsAsync(new List<Avistamiento> { Avistamiento(5, 1, "a borrar") });
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(Parametros(DatosDePrueba.Personaje(1)));

        await vm.Avistamientos[0].EliminarCommand.ExecuteAsync(null);

        _s.Avistamientos.Verify(a => a.EliminarAsync(5), Times.Once);
        _s.Fotos.Verify(f => f.Eliminar("/fotos/5.jpg"), Times.Once);
        Assert.Empty(vm.Avistamientos);
        Assert.Equal("Todavía no lo avistaste. Registrá el primero con una foto.", vm.ResumenAvistamientos);
    }

    [Fact]
    public async Task EliminarAvistamiento_SiLaBaseFalla_NoBorraLaFoto()
    {
        _s.Avistamientos.Setup(a => a.ObtenerPorPersonajeAsync(1))
            .ReturnsAsync(new List<Avistamiento> { Avistamiento(5, 1, "nota") });
        _s.Avistamientos.Setup(a => a.EliminarAsync(5)).ThrowsAsync(new InvalidOperationException("bloqueada"));
        var vm = _s.CrearDetalleViewModel();
        vm.Recibir(Parametros(DatosDePrueba.Personaje(1)));

        await vm.Avistamientos[0].EliminarCommand.ExecuteAsync(null);

        _s.Fotos.Verify(f => f.Eliminar(It.IsAny<string>()), Times.Never);
        Assert.Single(vm.Avistamientos);
        Assert.True(vm.HayError);
    }
}
