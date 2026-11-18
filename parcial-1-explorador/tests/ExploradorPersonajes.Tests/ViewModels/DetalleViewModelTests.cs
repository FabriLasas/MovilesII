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
}
