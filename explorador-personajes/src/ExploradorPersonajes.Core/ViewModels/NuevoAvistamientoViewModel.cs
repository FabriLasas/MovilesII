using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Validaciones;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// Formulario para registrar un avistamiento: foto (cámara o galería), nota
/// y, opcionalmente, la ubicación.
///
/// Criterios de permisos (Unidad 5):
/// - Se piden en el momento en que hacen falta, nunca todos juntos al abrir.
/// - Si se niegan, la app no falla: explica qué pasó y ofrece una
///   alternativa (galería en lugar de cámara, guardar sin ubicación) y un
///   acceso a Ajustes para habilitarlos.
/// </summary>
public partial class NuevoAvistamientoViewModel : ViewModelBase
{
    private static readonly TimeSpan VibracionAlGuardar = TimeSpan.FromMilliseconds(300);

    private readonly IServicioPermisos _permisos;
    private readonly IServicioFotos _fotos;
    private readonly IServicioUbicacion _ubicacion;
    private readonly IRepositorioAvistamientos _repositorio;
    private readonly IServicioHaptica _haptica;
    private readonly IServicioNavegacion _navegacion;
    private readonly IMessenger _mensajero;
    private readonly Func<DateTime> _ahora;

    public NuevoAvistamientoViewModel(
        IServicioPermisos permisos,
        IServicioFotos fotos,
        IServicioUbicacion ubicacion,
        IRepositorioAvistamientos repositorio,
        IServicioHaptica haptica,
        IServicioNavegacion navegacion,
        IMessenger mensajero,
        Func<DateTime>? ahora = null)
    {
        _permisos = permisos;
        _fotos = fotos;
        _ubicacion = ubicacion;
        _repositorio = repositorio;
        _haptica = haptica;
        _navegacion = navegacion;
        _mensajero = mensajero;
        _ahora = ahora ?? (() => DateTime.Now);

        Titulo = "Nuevo avistamiento";
        Nota = string.Empty;
        CamaraDisponible = fotos.CamaraDisponible;
        MostrarInfo("Sacá una foto o elegila de la galería.");
    }

    [ObservableProperty]
    public partial Personaje? Personaje { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneFoto))]
    [NotifyPropertyChangedFor(nameof(SinFoto))]
    public partial string? RutaFoto { get; set; }

    public bool TieneFoto => !string.IsNullOrEmpty(RutaFoto);

    /// <summary>Negación expuesta como propiedad para enlazarla sin un convertidor.</summary>
    public bool SinFoto => !TieneFoto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContadorNota))]
    public partial string Nota { get; set; }

    /// <summary>Caracteres usados sobre el máximo, como ayuda mientras se escribe.</summary>
    public string ContadorNota => $"{(Nota ?? string.Empty).Trim().Length}/{ValidadorAvistamiento.LargoMaximoNota}";

    public int LargoMaximoNota => ValidadorAvistamiento.LargoMaximoNota;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneUbicacion))]
    [NotifyPropertyChangedFor(nameof(TextoUbicacion))]
    public partial Coordenadas? Ubicacion { get; set; }

    public bool TieneUbicacion => Ubicacion is not null;

    public string TextoUbicacion => Ubicacion?.ToString() ?? "Sin ubicación (es opcional)";

    [ObservableProperty]
    public partial bool CamaraDisponible { get; set; }

    [ObservableProperty]
    public partial bool BuscandoUbicacion { get; set; }

    /// <summary>Muestra el acceso a Ajustes después de que se negó un permiso.</summary>
    [ObservableProperty]
    public partial bool MostrarAccesoAjustes { get; set; }

    // Errores de validación, cada uno debajo de su campo. Null = sin error.
    // Las propiedades HayError... controlan la visibilidad: un mensaje vacío
    // pero visible seguiría ocupando lugar en la pantalla.

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayErrorFoto))]
    public partial string? ErrorFoto { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayErrorNota))]
    public partial string? ErrorNota { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayErrorUbicacion))]
    public partial string? ErrorUbicacion { get; set; }

    public bool HayErrorFoto => ErrorFoto is not null;
    public bool HayErrorNota => ErrorNota is not null;
    public bool HayErrorUbicacion => ErrorUbicacion is not null;

    /// <summary>Recibe el personaje por navegación, igual que el detalle.</summary>
    public void Recibir(IDictionary<string, object> parametros)
    {
        if (parametros.TryGetValue(DetalleViewModel.ClaveParametro, out var valor) && valor is Personaje personaje)
        {
            Personaje = personaje;
        }
        else
        {
            MostrarError("No se recibió el personaje del avistamiento.");
        }
    }

    // --- Foto ---

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task TomarFotoAsync()
    {
        var permiso = await _permisos.SolicitarCamaraAsync();

        if (permiso == EstadoPermiso.NoDisponible)
        {
            MostrarError("Este dispositivo no permite usar la cámara. Elegí una foto de la galería.");
            return;
        }

        if (permiso == EstadoPermiso.Denegado)
        {
            MostrarAccesoAjustes = true;
            MostrarError("Sin permiso para usar la cámara. Podés elegir una foto de la galería o habilitar el permiso en Ajustes.");
            return;
        }

        await ObtenerFotoAsync(_fotos.TomarAsync);
    }

    /// <summary>
    /// La galería no pide permiso: el selector de fotos del sistema solo le
    /// entrega a la app la imagen que el usuario eligió. Por eso es la
    /// alternativa cuando se niega la cámara.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task ElegirFotoAsync() => ObtenerFotoAsync(_fotos.ElegirDeGaleriaAsync);

    private async Task ObtenerFotoAsync(Func<Task<string?>> obtener)
    {
        string? ruta;
        try
        {
            ruta = await obtener();
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo obtener la foto. ({ex.Message})");
            return;
        }

        // null = el usuario cerró la cámara o la galería sin elegir nada.
        if (ruta is null)
        {
            return;
        }

        // Si ya había una foto, la anterior se descarta del disco.
        if (RutaFoto is not null)
        {
            _fotos.Eliminar(RutaFoto);
        }

        RutaFoto = ruta;
        ErrorFoto = null;
        MostrarAccesoAjustes = false;
        MostrarInfo("Foto lista.");
    }

    // --- Ubicación ---

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task AgregarUbicacionAsync()
    {
        var permiso = await _permisos.SolicitarUbicacionAsync();

        // La ubicación es opcional: sin permiso no es un error, solo se avisa
        // que el avistamiento se guardará sin ella.
        if (permiso != EstadoPermiso.Concedido)
        {
            MostrarAccesoAjustes = permiso == EstadoPermiso.Denegado;
            MostrarInfo("Sin permiso de ubicación: el avistamiento se guardará sin ella.");
            return;
        }

        BuscandoUbicacion = true;
        MostrarInfo("Buscando tu ubicación...");

        var coordenadas = await _ubicacion.ObtenerAsync();

        BuscandoUbicacion = false;

        if (coordenadas is null)
        {
            MostrarInfo("No se pudo obtener la ubicación. Revisá que el GPS esté activado, o guardá sin ella.");
            return;
        }

        Ubicacion = coordenadas;
        ErrorUbicacion = null;
        MostrarAccesoAjustes = false;
        MostrarInfo("Ubicación agregada.");
    }

    [RelayCommand]
    private void QuitarUbicacion()
    {
        Ubicacion = null;
        ErrorUbicacion = null;
    }

    [RelayCommand]
    private void AbrirAjustes() => _permisos.AbrirAjustes();

    // --- Guardar y cancelar ---

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task GuardarAsync()
    {
        var avistamiento = new Avistamiento
        {
            PersonajeId = Personaje?.Id ?? 0,
            RutaFoto = RutaFoto ?? string.Empty,
            Nota = (Nota ?? string.Empty).Trim(),
            Latitud = Ubicacion?.Latitud,
            Longitud = Ubicacion?.Longitud,
            Fecha = _ahora()
        };

        // Todas las reglas están en el validador: el ViewModel solo reparte
        // los mensajes entre los campos de la pantalla.
        var validacion = ValidadorAvistamiento.Validar(avistamiento);
        ErrorFoto = validacion.ErrorDe(ValidadorAvistamiento.CampoFoto);
        ErrorNota = validacion.ErrorDe(ValidadorAvistamiento.CampoNota);
        ErrorUbicacion = validacion.ErrorDe(ValidadorAvistamiento.CampoUbicacion);

        if (!validacion.EsValido)
        {
            MostrarError(validacion.ErrorDe(ValidadorAvistamiento.CampoPersonaje) ?? "Revisá los datos marcados.");
            return;
        }

        // La foto recién sacada está en una carpeta temporal. Se copia a la
        // definitiva solo ahora que el avistamiento se va a guardar: así una
        // foto descartada (o abandonada con el botón atrás) nunca queda
        // ocupando lugar en la carpeta de la app.
        var temporal = avistamiento.RutaFoto;
        string? definitiva = null;

        EstaCargando = true;
        try
        {
            definitiva = await _fotos.ConservarAsync(temporal);
            avistamiento.RutaFoto = definitiva;
            await _repositorio.GuardarAsync(avistamiento);
        }
        catch (Exception ex)
        {
            // Si falló al guardar, se borra la copia: el avistamiento no existe
            // y la temporal sigue disponible para reintentar.
            if (definitiva is not null)
            {
                _fotos.Eliminar(definitiva);
            }

            MostrarError($"No se pudo guardar el avistamiento. ({ex.Message})");
            return;
        }
        finally
        {
            EstaCargando = false;
        }

        _fotos.Eliminar(temporal);
        RutaFoto = null;

        _haptica.Vibrar(VibracionAlGuardar);
        _mensajero.Send(new AvistamientoGuardadoMensaje(avistamiento));

        await _navegacion.VolverAsync();
    }

    /// <summary>Descarta el formulario y la foto que se hubiera sacado.</summary>
    [RelayCommand]
    private async Task CancelarAsync()
    {
        if (RutaFoto is not null)
        {
            _fotos.Eliminar(RutaFoto);
            RutaFoto = null;
        }

        await _navegacion.VolverAsync();
    }
}
