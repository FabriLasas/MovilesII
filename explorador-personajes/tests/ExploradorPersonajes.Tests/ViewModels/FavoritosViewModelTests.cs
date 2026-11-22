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

    /// <summary>Simula favoritos guardados en el dispositivo.</summary>
    private List<Personaje> ConGuardados(params int[] ids)
    {
        var guardados = ids.Select(id => DatosDePrueba.Personaje(id, $"Guardado {id}")).ToList();
        _s.Favoritos.Setup(f => f.ObtenerTodos()).Returns(() => guardados);
        foreach (var id in ids)
        {
            _s.Favoritos.Setup(f => f.EsFavorito(id)).Returns(true);
        }

        return guardados;
    }

    private void ApiResponde(ResultadoApi<List<Personaje>> resultado)
        => _s.Api.Setup(a => a.ObtenerPersonajesPorIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(resultado);

    [Fact]
    public void AlCrearse_MuestraLosGuardadosEnSuOrdenYRefrescaConLaApi()
    {
        ConGuardados(2, 1);
        var actuales = new List<Personaje> { DatosDePrueba.Personaje(2, "Morty"), DatosDePrueba.Personaje(1, "Rick") };
        ApiResponde(ResultadoApi<List<Personaje>>.Exito(actuales));

        var vm = _s.CrearFavoritosViewModel();

        Assert.Equal(new[] { 2, 1 }, vm.Favoritos.Select(f => f.Id));
        Assert.Equal("2 personajes favoritos.", vm.MensajeEstado);
        Assert.All(vm.Favoritos, f => Assert.True(f.EsFavorito));
        _s.Api.Verify(a => a.ObtenerPersonajesPorIdsAsync(
            It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 2, 1 })), It.IsAny<CancellationToken>()), Times.Once);
        _s.Favoritos.Verify(f => f.ActualizarDatosAsync(actuales), Times.Once);
    }

    [Fact]
    public void AlCrearse_SinConexion_MuestraLosGuardadosSinMarcarError()
    {
        ConGuardados(1, 2);
        ApiResponde(ResultadoApi<List<Personaje>>.Fallo(TipoError.SinConexion, "No hay conexión a internet."));

        var vm = _s.CrearFavoritosViewModel();

        Assert.Equal(new[] { "Guardado 1", "Guardado 2" }, vm.Favoritos.Select(f => f.Nombre));
        Assert.False(vm.HayError);
        Assert.Equal("No se pudieron actualizar los datos: se muestran los guardados en el dispositivo.", vm.MensajeEstado);
        _s.Favoritos.Verify(f => f.ActualizarDatosAsync(It.IsAny<IEnumerable<Personaje>>()), Times.Never);
    }

    [Fact]
    public void AlCrearse_RespuestaDesdeLaCache_NoPisaLosDatosGuardados()
    {
        // La copia de la caché puede ser más vieja que lo guardado en la base.
        ConGuardados(1);
        ApiResponde(ResultadoApi<List<Personaje>>.ExitoDesdeCache(
            new List<Personaje> { DatosDePrueba.Personaje(1, "Viejo") }, DateTime.Now, TipoError.SinConexion, "sin red"));

        var vm = _s.CrearFavoritosViewModel();

        Assert.Equal("Guardado 1", Assert.Single(vm.Favoritos).Nombre);
        _s.Favoritos.Verify(f => f.ActualizarDatosAsync(It.IsAny<IEnumerable<Personaje>>()), Times.Never);
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
        ConGuardados(1, 2);
        ApiResponde(ResultadoApi<List<Personaje>>.Fallo(TipoError.SinConexion, "sin red"));
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
