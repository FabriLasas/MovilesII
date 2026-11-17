using ExploradorPersonajes.Tests.Dobles;
using ExploradorPersonajes.ViewModels;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

public class PersonajeItemViewModelTests
{
    private readonly ServiciosSimulados _s = new();

    [Fact]
    public void AlternarFavorito_ActualizaLaEstrellaYPideLaVibracion()
    {
        var personaje = DatosDePrueba.Personaje(1);
        _s.Favoritos.Setup(f => f.Alternar(personaje)).Returns(true);
        var item = new PersonajeItemViewModel(personaje, _s.DeItem());

        item.AlternarFavoritoCommand.Execute(null);

        Assert.True(item.EsFavorito);
        Assert.Equal("★", item.IconoFavorito);
        Assert.Equal("Quitar", item.TextoAccionFavorito);
        _s.Haptica.Verify(h => h.ConfirmarToque(), Times.Once);
    }

    [Fact]
    public async Task VerDetalle_NavegaConElPersonajeDeLaTarjeta()
    {
        var personaje = DatosDePrueba.Personaje(5);
        var item = new PersonajeItemViewModel(personaje, _s.DeItem());

        await item.VerDetalleCommand.ExecuteAsync(null);

        _s.Navegacion.Verify(n => n.IrADetalleAsync(personaje), Times.Once);
    }

    [Fact]
    public void AlCrearse_TomaElEstadoDeFavoritoDelServicio()
    {
        _s.Favoritos.Setup(f => f.EsFavorito(8)).Returns(true);

        var item = new PersonajeItemViewModel(DatosDePrueba.Personaje(8), _s.DeItem());

        Assert.True(item.EsFavorito);
        Assert.Equal("☆", new PersonajeItemViewModel(DatosDePrueba.Personaje(9), _s.DeItem()).IconoFavorito);
    }
}
