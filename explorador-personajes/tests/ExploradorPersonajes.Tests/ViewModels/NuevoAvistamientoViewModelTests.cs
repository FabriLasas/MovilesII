using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Tests.Dobles;
using ExploradorPersonajes.ViewModels;
using Moq;

namespace ExploradorPersonajes.Tests.ViewModels;

/// <summary>
/// Formulario de avistamiento con la cámara, el GPS y los permisos
/// simulados: se puede probar qué hace la app cuando el usuario niega un
/// permiso o el GPS no responde, cosas difíciles de reproducir a mano.
/// </summary>
public class NuevoAvistamientoViewModelTests
{
    private const string Foto = "/cache/fotos-pendientes/a.jpg";
    private const string FotoDefinitiva = "/datos/fotos/a.jpg";

    private readonly ServiciosSimulados _s = new();

    public NuevoAvistamientoViewModelTests()
    {
        _s.Fotos.Setup(f => f.ConservarAsync(Foto)).ReturnsAsync(FotoDefinitiva);
    }

    private NuevoAvistamientoViewModel CrearConPersonaje(int id = 1)
    {
        var vm = _s.CrearNuevoAvistamientoViewModel();
        vm.Recibir(new Dictionary<string, object> { [DetalleViewModel.ClaveParametro] = DatosDePrueba.Personaje(id, "Rick Sanchez") });
        return vm;
    }

    private void PermisoDeCamara(EstadoPermiso estado) => _s.Permisos.Setup(p => p.SolicitarCamaraAsync()).ReturnsAsync(estado);

    private void PermisoDeUbicacion(EstadoPermiso estado) => _s.Permisos.Setup(p => p.SolicitarUbicacionAsync()).ReturnsAsync(estado);

    // --- Cámara ---

    [Fact]
    public async Task TomarFoto_ConPermiso_GuardaLaRutaDeLaFoto()
    {
        PermisoDeCamara(EstadoPermiso.Concedido);
        _s.Fotos.Setup(f => f.TomarAsync()).ReturnsAsync(Foto);
        var vm = CrearConPersonaje();

        await vm.TomarFotoCommand.ExecuteAsync(null);

        Assert.Equal(Foto, vm.RutaFoto);
        Assert.True(vm.TieneFoto);
        Assert.False(vm.SinFoto);
        Assert.False(vm.HayError);
    }

    [Fact]
    public async Task TomarFoto_PermisoDenegado_NoAbreLaCamaraYOfreceAlternativas()
    {
        PermisoDeCamara(EstadoPermiso.Denegado);
        var vm = CrearConPersonaje();

        await vm.TomarFotoCommand.ExecuteAsync(null);

        _s.Fotos.Verify(f => f.TomarAsync(), Times.Never);
        Assert.True(vm.HayError);
        Assert.Contains("galería", vm.MensajeEstado);
        Assert.True(vm.MostrarAccesoAjustes);
    }

    [Fact]
    public async Task TomarFoto_SinCamara_SugiereLaGaleriaSinOfrecerAjustes()
    {
        PermisoDeCamara(EstadoPermiso.NoDisponible);
        var vm = CrearConPersonaje();

        await vm.TomarFotoCommand.ExecuteAsync(null);

        Assert.Equal("Este dispositivo no permite usar la cámara. Elegí una foto de la galería.", vm.MensajeEstado);
        Assert.False(vm.MostrarAccesoAjustes);
    }

    [Fact]
    public async Task TomarFoto_UsuarioCancela_NoCambiaNada()
    {
        PermisoDeCamara(EstadoPermiso.Concedido);
        _s.Fotos.Setup(f => f.TomarAsync()).ReturnsAsync((string?)null);
        var vm = CrearConPersonaje();

        await vm.TomarFotoCommand.ExecuteAsync(null);

        Assert.Null(vm.RutaFoto);
        Assert.False(vm.HayError);
    }

    [Fact]
    public async Task TomarFoto_LaCamaraFalla_MuestraElErrorSinCerrarLaApp()
    {
        PermisoDeCamara(EstadoPermiso.Concedido);
        _s.Fotos.Setup(f => f.TomarAsync()).ThrowsAsync(new IOException("disco lleno"));
        var vm = CrearConPersonaje();

        await vm.TomarFotoCommand.ExecuteAsync(null);

        Assert.True(vm.HayError);
        Assert.Equal("No se pudo obtener la foto. (disco lleno)", vm.MensajeEstado);
    }

    [Fact]
    public async Task ElegirFoto_ReemplazaLaAnteriorYLaBorraDelDisco()
    {
        _s.Fotos.SetupSequence(f => f.ElegirDeGaleriaAsync()).ReturnsAsync("/f/1.jpg").ReturnsAsync("/f/2.jpg");
        var vm = CrearConPersonaje();

        await vm.ElegirFotoCommand.ExecuteAsync(null);
        await vm.ElegirFotoCommand.ExecuteAsync(null);

        Assert.Equal("/f/2.jpg", vm.RutaFoto);
        _s.Fotos.Verify(f => f.Eliminar("/f/1.jpg"), Times.Once);
        _s.Permisos.Verify(p => p.SolicitarCamaraAsync(), Times.Never);
    }

    [Fact]
    public void AlCrearse_SinCamara_OcultaElBotonDeLaCamara()
    {
        _s.Fotos.SetupGet(f => f.CamaraDisponible).Returns(false);

        Assert.False(_s.CrearNuevoAvistamientoViewModel().CamaraDisponible);
    }

    // --- Ubicación ---

    [Fact]
    public async Task AgregarUbicacion_ConPermiso_GuardaLasCoordenadas()
    {
        PermisoDeUbicacion(EstadoPermiso.Concedido);
        _s.Ubicacion.Setup(u => u.ObtenerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Coordenadas(-34.6037, -58.3816));
        var vm = CrearConPersonaje();

        await vm.AgregarUbicacionCommand.ExecuteAsync(null);

        Assert.True(vm.TieneUbicacion);
        Assert.Equal("-34.60370, -58.38160", vm.TextoUbicacion);
        Assert.False(vm.BuscandoUbicacion);
    }

    [Fact]
    public async Task AgregarUbicacion_PermisoDenegado_AvisaQueSeGuardaraSinUbicacionSinMarcarError()
    {
        PermisoDeUbicacion(EstadoPermiso.Denegado);
        var vm = CrearConPersonaje();

        await vm.AgregarUbicacionCommand.ExecuteAsync(null);

        _s.Ubicacion.Verify(u => u.ObtenerAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(vm.HayError);
        Assert.Equal("Sin permiso de ubicación: el avistamiento se guardará sin ella.", vm.MensajeEstado);
        Assert.True(vm.MostrarAccesoAjustes);
        Assert.Equal("Sin ubicación (es opcional)", vm.TextoUbicacion);
    }

    [Fact]
    public async Task AgregarUbicacion_GpsSinRespuesta_SugiereRevisarElGps()
    {
        PermisoDeUbicacion(EstadoPermiso.Concedido);
        _s.Ubicacion.Setup(u => u.ObtenerAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Coordenadas?)null);
        var vm = CrearConPersonaje();

        await vm.AgregarUbicacionCommand.ExecuteAsync(null);

        Assert.False(vm.TieneUbicacion);
        Assert.Contains("GPS", vm.MensajeEstado);
        Assert.False(vm.BuscandoUbicacion);
    }

    [Fact]
    public async Task QuitarUbicacion_LaBorra()
    {
        PermisoDeUbicacion(EstadoPermiso.Concedido);
        _s.Ubicacion.Setup(u => u.ObtenerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Coordenadas(1, 2));
        var vm = CrearConPersonaje();
        await vm.AgregarUbicacionCommand.ExecuteAsync(null);

        vm.QuitarUbicacionCommand.Execute(null);

        Assert.False(vm.TieneUbicacion);
    }

    [Fact]
    public void AbrirAjustes_DelegaEnElServicioDePermisos()
    {
        CrearConPersonaje().AbrirAjustesCommand.Execute(null);

        _s.Permisos.Verify(p => p.AbrirAjustes(), Times.Once);
    }

    // --- Guardar ---

    [Fact]
    public async Task Guardar_SinFotoNiNota_MarcaCadaCampoYNoGuarda()
    {
        var vm = CrearConPersonaje();

        await vm.GuardarCommand.ExecuteAsync(null);

        Assert.Equal("Sacá una foto o elegila de la galería.", vm.ErrorFoto);
        Assert.Equal("Completá la nota.", vm.ErrorNota);
        Assert.Null(vm.ErrorUbicacion);
        Assert.True(vm.HayErrorFoto);
        Assert.True(vm.HayErrorNota);
        Assert.False(vm.HayErrorUbicacion);
        Assert.True(vm.HayError);
        Assert.Equal("Revisá los datos marcados.", vm.MensajeEstado);
        _s.Avistamientos.Verify(a => a.GuardarAsync(It.IsAny<Avistamiento>()), Times.Never);
        _s.Navegacion.Verify(n => n.VolverAsync(), Times.Never);
    }

    [Fact]
    public async Task Guardar_Valido_GuardaVibraAvisaYVuelve()
    {
        _s.Fotos.Setup(f => f.ElegirDeGaleriaAsync()).ReturnsAsync(Foto);
        PermisoDeUbicacion(EstadoPermiso.Concedido);
        _s.Ubicacion.Setup(u => u.ObtenerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Coordenadas(-34.6, -58.4));
        var avisos = new List<AvistamientoGuardadoMensaje>();
        _s.Mensajero.Register<AvistamientoGuardadoMensaje>(this, (_, m) => avisos.Add(m));
        var vm = CrearConPersonaje(id: 7);
        await vm.ElegirFotoCommand.ExecuteAsync(null);
        await vm.AgregarUbicacionCommand.ExecuteAsync(null);
        vm.Nota = "  Lo vi en la plaza  ";

        await vm.GuardarCommand.ExecuteAsync(null);

        _s.Avistamientos.Verify(a => a.GuardarAsync(It.Is<Avistamiento>(x =>
            x.PersonajeId == 7 &&
            x.RutaFoto == FotoDefinitiva &&
            x.Nota == "Lo vi en la plaza" &&
            x.Latitud == -34.6 && x.Longitud == -58.4 &&
            x.Fecha == ServiciosSimulados.Ahora)), Times.Once);
        _s.Haptica.Verify(h => h.Vibrar(TimeSpan.FromMilliseconds(300)), Times.Once);
        Assert.Equal(7, Assert.Single(avisos).Avistamiento.PersonajeId);
        _s.Navegacion.Verify(n => n.VolverAsync(), Times.Once);

        // La foto pasó de la carpeta temporal a la definitiva.
        _s.Fotos.Verify(f => f.Eliminar(Foto), Times.Once);
        _s.Fotos.Verify(f => f.Eliminar(FotoDefinitiva), Times.Never);
    }

    [Fact]
    public async Task Guardar_SinUbicacion_TambienSeGuarda()
    {
        _s.Fotos.Setup(f => f.ElegirDeGaleriaAsync()).ReturnsAsync(Foto);
        var vm = CrearConPersonaje();
        await vm.ElegirFotoCommand.ExecuteAsync(null);
        vm.Nota = "Sin GPS";

        await vm.GuardarCommand.ExecuteAsync(null);

        _s.Avistamientos.Verify(a => a.GuardarAsync(It.Is<Avistamiento>(x => x.Latitud == null && x.Longitud == null)), Times.Once);
    }

    [Fact]
    public async Task Guardar_LaBaseFalla_MuestraElErrorYNoVuelve()
    {
        _s.Fotos.Setup(f => f.ElegirDeGaleriaAsync()).ReturnsAsync(Foto);
        _s.Avistamientos.Setup(a => a.GuardarAsync(It.IsAny<Avistamiento>())).ThrowsAsync(new InvalidOperationException("base bloqueada"));
        var vm = CrearConPersonaje();
        await vm.ElegirFotoCommand.ExecuteAsync(null);
        vm.Nota = "Nota válida";

        await vm.GuardarCommand.ExecuteAsync(null);

        Assert.True(vm.HayError);
        Assert.Equal("No se pudo guardar el avistamiento. (base bloqueada)", vm.MensajeEstado);
        Assert.False(vm.EstaCargando);
        _s.Navegacion.Verify(n => n.VolverAsync(), Times.Never);

        // Se descarta la copia definitiva y se conserva la temporal para reintentar.
        _s.Fotos.Verify(f => f.Eliminar(FotoDefinitiva), Times.Once);
        _s.Fotos.Verify(f => f.Eliminar(Foto), Times.Never);
        Assert.Equal(Foto, vm.RutaFoto);
    }

    [Fact]
    public async Task Cancelar_DescartaLaFotoSacadaYVuelve()
    {
        _s.Fotos.Setup(f => f.ElegirDeGaleriaAsync()).ReturnsAsync(Foto);
        var vm = CrearConPersonaje();
        await vm.ElegirFotoCommand.ExecuteAsync(null);

        await vm.CancelarCommand.ExecuteAsync(null);

        _s.Fotos.Verify(f => f.Eliminar(Foto), Times.Once);
        _s.Navegacion.Verify(n => n.VolverAsync(), Times.Once);
    }

    [Fact]
    public void ContadorNota_CuentaSinLosEspaciosDeLosExtremos()
    {
        var vm = CrearConPersonaje();

        vm.Nota = "  hola  ";

        Assert.Equal("4/200", vm.ContadorNota);
    }

    [Fact]
    public void Recibir_SinPersonaje_InformaElError()
    {
        var vm = _s.CrearNuevoAvistamientoViewModel();

        vm.Recibir(new Dictionary<string, object>());

        Assert.True(vm.HayError);
    }
}
