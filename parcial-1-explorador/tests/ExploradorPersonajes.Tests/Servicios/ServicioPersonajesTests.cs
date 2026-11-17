using System.Net;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using Moq;

namespace ExploradorPersonajes.Tests.Servicios;

/// <summary>
/// Pruebas del único punto que habla con la red. Verifican que cada situación
/// (respuesta correcta, 404, 500, JSON dañado, timeout, sin red) termine en el
/// tipo de error correcto, que es lo que después decide el mensaje al usuario.
///
/// La red se reemplaza por <see cref="ManejadorHttpFalso"/>; la conectividad y
/// la caché, por mocks de Moq.
/// </summary>
public class ServicioPersonajesTests
{
    private readonly Mock<IServicioConectividad> _conectividad = new();
    private readonly Mock<IServicioCache> _cache = new();

    public ServicioPersonajesTests()
    {
        // Por defecto: hay red y la caché está vacía.
        _conectividad.SetupGet(c => c.SinRedConfirmada).Returns(false);
        _conectividad.SetupGet(c => c.SistemaInformaInternet).Returns(true);
        _cache.Setup(c => c.LeerAsync(It.IsAny<string>())).ReturnsAsync((EntradaCache?)null);
    }

    private ServicioPersonajes CrearServicio(ManejadorHttpFalso manejador)
        => new(new HttpClient(manejador), _conectividad.Object, _cache.Object);

    // --- Respuestas exitosas ---

    [Fact]
    public async Task ObtenerPersonajes_Respuesta200_DevuelveLosPersonajesDeserializados()
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, DatosDePrueba.JsonPagina(826, true, 1, 2)));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.True(resultado.EsExitoso);
        Assert.Equal(TipoError.Ninguno, resultado.Error);
        Assert.False(resultado.DesdeCache);
        Assert.Equal(826, resultado.Datos!.Info!.TotalElementos);
        Assert.Equal(new[] { 1, 2 }, resultado.Datos.Resultados.Select(p => p.Id));
        Assert.Equal("Earth (C-137)", resultado.Datos.Resultados[0].Origen!.Nombre);
    }

    [Fact]
    public async Task ObtenerPersonajes_Respuesta200_GuardaLaRespuestaEnCache()
    {
        var json = DatosDePrueba.JsonPagina(826, true, 1);
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, json));

        await servicio.ObtenerPersonajesAsync();

        _cache.Verify(c => c.GuardarAsync("character/?page=1", json), Times.Once);
    }

    [Fact]
    public async Task ObtenerPersonajes_ConFiltros_ArmaLaRutaConElNombreEscapado()
    {
        var manejador = ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, DatosDePrueba.JsonPagina(1, false, 1));
        var servicio = CrearServicio(manejador);

        await servicio.ObtenerPersonajesAsync(pagina: 2, nombre: "  rick sanchez ", estado: "alive");

        Assert.Equal("character/?page=2&name=rick%20sanchez&status=alive", manejador.RutasPedidas.Single());
    }

    [Fact]
    public async Task ObtenerPersonajes_SinFiltros_NoAgregaParametrosVacios()
    {
        var manejador = ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, DatosDePrueba.JsonPagina(1, false, 1));
        var servicio = CrearServicio(manejador);

        await servicio.ObtenerPersonajesAsync(nombre: "   ", estado: null);

        Assert.Equal("character/?page=1", manejador.RutasPedidas.Single());
    }

    // --- Errores HTTP ---

    [Fact]
    public async Task ObtenerPersonajes_Respuesta404_EsUnaBusquedaSinResultados()
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.NotFound));

        var resultado = await servicio.ObtenerPersonajesAsync(nombre: "zzzz");

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.SinResultados, resultado.Error);
        Assert.Equal(404, resultado.CodigoHttp);
        Assert.Equal("No se encontraron personajes para esa búsqueda.", resultado.Mensaje);
    }

    [Fact]
    public async Task ObtenerPersonajePorId_Respuesta404_EsUnRecursoInexistente()
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.NotFound));

        var resultado = await servicio.ObtenerPersonajePorIdAsync(9999);

        Assert.Equal(TipoError.NoEncontrado, resultado.Error);
        Assert.Equal("El personaje solicitado no existe. (Error 404)", resultado.Mensaje);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, TipoError.ErrorCliente)]
    [InlineData(HttpStatusCode.Unauthorized, TipoError.ErrorCliente)]
    [InlineData(HttpStatusCode.TooManyRequests, TipoError.ErrorCliente)]
    [InlineData(HttpStatusCode.InternalServerError, TipoError.ErrorServidor)]
    [InlineData(HttpStatusCode.ServiceUnavailable, TipoError.ErrorServidor)]
    public async Task ObtenerPersonajes_CodigoDeError_SeClasificaSegunSuFamilia(HttpStatusCode codigo, TipoError esperado)
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(codigo));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.False(resultado.EsExitoso);
        Assert.Equal(esperado, resultado.Error);
        Assert.Equal((int)codigo, resultado.CodigoHttp);
        Assert.Contains($"(Error {(int)codigo})", resultado.Mensaje);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ObtenerPersonajes_ErrorDelServidor_NuncaSeReemplazaPorLaCache(HttpStatusCode codigo)
    {
        // Un 404 o un 500 son respuestas reales: mostrar la copia guardada
        // escondería el problema.
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(codigo));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.False(resultado.DesdeCache);
        _cache.Verify(c => c.LeerAsync(It.IsAny<string>()), Times.Never);
    }

    // --- Respuestas dañadas ---

    [Theory]
    [InlineData("{ esto no es json")]
    [InlineData("null")]
    public async Task ObtenerPersonajes_CuerpoInvalido_DevuelveRespuestaInvalida(string cuerpo)
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, cuerpo));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.Equal(TipoError.RespuestaInvalida, resultado.Error);
        _cache.Verify(c => c.GuardarAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- Problemas de conectividad ---

    [Fact]
    public async Task ObtenerPersonajes_Timeout_DevuelveTiempoAgotado()
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueLance(new TaskCanceledException()));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.Equal(TipoError.TiempoAgotado, resultado.Error);
    }

    [Fact]
    public async Task ObtenerPersonajes_FalloDeTransporteConInternet_DevuelveFalloDeRed()
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueLance(new HttpRequestException("DNS")));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.Equal(TipoError.FalloDeRed, resultado.Error);
        Assert.Contains("No se pudo contactar al servidor", resultado.Mensaje);
    }

    [Fact]
    public async Task ObtenerPersonajes_FalloDeTransporteSinInternet_DevuelveSinConexion()
    {
        _conectividad.SetupGet(c => c.SistemaInformaInternet).Returns(false);
        var servicio = CrearServicio(ManejadorHttpFalso.QueLance(new HttpRequestException("sin red")));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.Equal(TipoError.SinConexion, resultado.Error);
    }

    [Fact]
    public async Task ObtenerPersonajes_SinRedConfirmada_NoIntentaLaPeticion()
    {
        _conectividad.SetupGet(c => c.SinRedConfirmada).Returns(true);
        var manejador = ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, DatosDePrueba.JsonPagina(1, false, 1));
        var servicio = CrearServicio(manejador);

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.Empty(manejador.RutasPedidas);
        Assert.Equal(TipoError.SinConexion, resultado.Error);
        Assert.False(resultado.EsExitoso);
    }

    [Fact]
    public async Task ObtenerPersonajes_SinRedConCopiaGuardada_DevuelveLaCopiaConSuFecha()
    {
        var fecha = new DateTime(2026, 11, 10, 18, 30, 0);
        _conectividad.SetupGet(c => c.SinRedConfirmada).Returns(true);
        _cache.Setup(c => c.LeerAsync("character/?page=1"))
              .ReturnsAsync(new EntradaCache(DatosDePrueba.JsonPagina(826, true, 1, 2), fecha));
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.OK));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.True(resultado.EsExitoso);
        Assert.True(resultado.DesdeCache);
        Assert.Equal(fecha, resultado.FechaCache);
        Assert.Equal(TipoError.SinConexion, resultado.Error);
        Assert.Equal(2, resultado.Datos!.Resultados.Count);
    }

    [Fact]
    public async Task ObtenerPersonajes_TimeoutConCopiaGuardada_DevuelveLaCopia()
    {
        _cache.Setup(c => c.LeerAsync(It.IsAny<string>()))
              .ReturnsAsync(new EntradaCache(DatosDePrueba.JsonPagina(826, true, 1), DateTime.Now));
        var servicio = CrearServicio(ManejadorHttpFalso.QueLance(new TaskCanceledException()));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.True(resultado.DesdeCache);
        Assert.Equal(TipoError.TiempoAgotado, resultado.Error);
    }

    [Fact]
    public async Task ObtenerPersonajes_CopiaGuardadaDaniada_InformaElErrorOriginal()
    {
        _conectividad.SetupGet(c => c.SinRedConfirmada).Returns(true);
        _cache.Setup(c => c.LeerAsync(It.IsAny<string>()))
              .ReturnsAsync(new EntradaCache("{ dañado", DateTime.Now));
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.OK));

        var resultado = await servicio.ObtenerPersonajesAsync();

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.SinConexion, resultado.Error);
    }

    [Fact]
    public async Task ObtenerPersonajes_CanceladoPorElUsuario_PropagaLaCancelacion()
    {
        // La cancelación que pide el propio usuario (siguió escribiendo) no es
        // un error a mostrar: el ViewModel la espera como excepción y la ignora.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.OK));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servicio.ObtenerPersonajesAsync(ct: cts.Token));
    }

    // --- Varios personajes por id (pestaña Favoritos) ---

    [Fact]
    public async Task ObtenerPorIds_VariosIds_RespetaElOrdenPedidoYOmiteLosInexistentes()
    {
        // La API devuelve ordenado por id y omite en silencio el 9999.
        var manejador = ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, DatosDePrueba.JsonLista(1, 2, 5));
        var servicio = CrearServicio(manejador);

        var resultado = await servicio.ObtenerPersonajesPorIdsAsync(new[] { 5, 9999, 1, 2 });

        Assert.Equal("character/5,9999,1,2", manejador.RutasPedidas.Single());
        Assert.Equal(new[] { 5, 1, 2 }, resultado.Datos!.Select(p => p.Id));
    }

    [Fact]
    public async Task ObtenerPorIds_UnSoloId_PideElObjetoIndividualYLoEnvuelveEnLista()
    {
        // Con un solo id la API devuelve un objeto y no un array.
        var manejador = ManejadorHttpFalso.QueResponda(HttpStatusCode.OK, DatosDePrueba.JsonPersonaje(7, "Bird Person"));
        var servicio = CrearServicio(manejador);

        var resultado = await servicio.ObtenerPersonajesPorIdsAsync(new[] { 7 });

        Assert.Equal("character/7", manejador.RutasPedidas.Single());
        Assert.Equal("Bird Person", Assert.Single(resultado.Datos!).Nombre);
    }

    [Fact]
    public async Task ObtenerPorIds_UnicoIdInexistente_DevuelveListaVaciaSinError()
    {
        var servicio = CrearServicio(ManejadorHttpFalso.QueResponda(HttpStatusCode.NotFound));

        var resultado = await servicio.ObtenerPersonajesPorIdsAsync(new[] { 9999 });

        Assert.True(resultado.EsExitoso);
        Assert.Empty(resultado.Datos!);
    }

    [Fact]
    public async Task ObtenerPorIds_ListaVacia_NoHaceNingunaPeticion()
    {
        var manejador = ManejadorHttpFalso.QueResponda(HttpStatusCode.OK);
        var servicio = CrearServicio(manejador);

        var resultado = await servicio.ObtenerPersonajesPorIdsAsync(Array.Empty<int>());

        Assert.True(resultado.EsExitoso);
        Assert.Empty(manejador.RutasPedidas);
    }
}
