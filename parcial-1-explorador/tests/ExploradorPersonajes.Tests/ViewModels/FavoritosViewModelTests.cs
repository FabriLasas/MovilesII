using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

public class FavoritosViewModelTests
{
    private readonly ServiciosSimulados _s = new();

    private void ConFavoritos(params int[] ids)
    {
        _s.Favoritos.Setup(f => f.ObtenerIds()).Returns(ids);
        foreach (var id in ids)
        {
            _s.Favoritos.Setup(f => f.EsFavorito(id)).Returns(true);
        }

        _s.Api.Setup(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(ResultadoApi<List<Personaje>>.Exito(ids.Select(id => DatosDePrueba.Personaje(id)).ToList()));
    }

    [Fact]
    public void AlCrearse_CargaLosFavoritosEnElOrdenEnQueSeMarcaron()
    {
        ConFavoritos(2, 1);

        var vm = _s.CrearFavoritosViewModel();

        Assert.Equal(new[] { 2, 1 }, vm.Favoritos.Select(f => f.Id));
        Assert.Equal("2 personajes favoritos.", vm.MensajeEstado);
        Assert.All(vm.Favoritos, f => Assert.True(f.EsFavorito));
    }

    [Fact]
    public void AlCrearse_SinFavoritos_NoConsultaLaApi()
    {
        var vm = _s.CrearFavoritosViewModel();

        Assert.Empty(vm.Favoritos);
        Assert.Equal("Todavía no marcaste favoritos. Tocá la estrella de un personaje.", vm.MensajeEstado);
        _s.Api.VerifyNoOtherCalls();
    }

    [Fact]
    public void AlCrearse_ErrorDeLaApi_LoMuestraComoError()
    {
        _s.Favoritos.Setup(f => f.ObtenerIds()).Returns(new[] { 1 });
        _s.Api.Setup(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(ResultadoApi<List<Personaje>>.Fallo(TipoError.SinConexion, "No hay conexión a internet."));

        var vm = _s.CrearFavoritosViewModel();

        Assert.True(vm.HayError);
        Assert.Equal("No hay conexión a internet.", vm.MensajeEstado);
    }

    [Fact]
    public void MensajeDeFavorito_Agregado_LoSumaSinVolverAConsultarLaApi()
    {
        var vm = _s.CrearFavoritosViewModel();

        _s.Mensajero.Send(new FavoritoCambiadoMensaje(DatosDePrueba.Personaje(7, "Bird Person"), true));

        Assert.Equal("Bird Person", Assert.Single(vm.Favoritos).Nombre);
        Assert.Equal("1 personaje favorito.", vm.MensajeEstado);
        _s.Api.VerifyNoOtherCalls();
    }

    [Fact]
    public void MensajeDeFavorito_Quitado_LoSacaDeLaLista()
    {
        ConFavoritos(1, 2);
        var vm = _s.CrearFavoritosViewModel();

        _s.Mensajero.Send(new FavoritoCambiadoMensaje(DatosDePrueba.Personaje(1), false));

        Assert.Equal(2, Assert.Single(vm.Favoritos).Id);
    }

    [Fact]
    public void MensajeDeFavorito_Repetido_NoLoDuplica()
    {
        var vm = _s.CrearFavoritosViewModel();
        var personaje = DatosDePrueba.Personaje(3);

        _s.Mensajero.Send(new FavoritoCambiadoMensaje(personaje, true));
        _s.Mensajero.Send(new FavoritoCambiadoMensaje(personaje, true));

        Assert.Single(vm.Favoritos);
    }
}
