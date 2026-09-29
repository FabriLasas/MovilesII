using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pantalla principal. Concentra el estado y las acciones de
/// la lista de personajes: carga, búsqueda, filtrado y paginación.
/// No conoce ningún control de la interfaz: se comunica con la vista sólo a
/// través de propiedades enlazadas y comandos.
/// </summary>
public partial class PersonajesViewModel : ViewModelBase, IRecipient<FavoritoCambiadoMensaje>
{
    /// <summary>
    /// Espera desde la última tecla antes de consultar a la API. Sin esta
    /// demora, escribir "rick" dispararía cuatro llamadas de red en lugar de
    /// una: tres inútiles y una correcta.
    /// </summary>
    private const int MilisegundosDeEspera = 500;

    private readonly IServicioPersonajes _servicio;
    private readonly ServiciosDeItem _serviciosDeItem;
    private readonly IServicioConectividad _conectividad;

    /// <summary>
    /// Permite cancelar la búsqueda pendiente cuando el usuario sigue
    /// escribiendo, y también la llamada en curso si ya había salido.
    /// </summary>
    private CancellationTokenSource? _cancelacionBusqueda;

    private int _paginaActual;
    private bool _hayPaginaSiguiente;

    /// <summary>
    /// Número de la consulta vigente. Cada búsqueda nueva lo incrementa; una
    /// respuesta que vuelve con un número viejo pertenece a una consulta que
    /// ya no se muestra y se descarta sin tocar la pantalla.
    /// </summary>
    private int _versionConsulta;

    /// <summary>
    /// Evita que las asignaciones del constructor disparen una búsqueda antes
    /// de que la pantalla esté lista.
    /// </summary>
    private readonly bool _inicializado;

    /// <summary>
    /// ObservableCollection (y no List) porque notifica altas y bajas a la
    /// interfaz: el CollectionView se actualiza solo al agregar elementos.
    /// </summary>
    public ObservableCollection<PersonajeItemViewModel> Personajes { get; } = new();

    public IReadOnlyList<OpcionFiltro> OpcionesFiltro { get; }

    [ObservableProperty]
    public partial string TextoBusqueda { get; set; }

    [ObservableProperty]
    public partial OpcionFiltro? FiltroSeleccionado { get; set; }

    [ObservableProperty]
    public partial bool EstaRefrescando { get; set; }

    [ObservableProperty]
    public partial bool EstaCargandoMas { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoSimulacion))]
    public partial bool SimulandoSinConexion { get; set; }

    public string TextoSimulacion => SimulandoSinConexion ? "Restaurar red" : "Simular sin red";

    public PersonajesViewModel(
        IServicioPersonajes servicio,
        ServiciosDeItem serviciosDeItem,
        IServicioConectividad conectividad,
        IMessenger mensajero)
    {
        _servicio = servicio;
        _serviciosDeItem = serviciosDeItem;
        _conectividad = conectividad;
        Titulo = "Personajes";
        TextoBusqueda = string.Empty;

        OpcionesFiltro = new List<OpcionFiltro>
        {
            new("Todos", null),
            new("Vivos", "alive"),
            new("Muertos", "dead"),
            new("Desconocidos", "unknown")
        };
        FiltroSeleccionado = OpcionesFiltro[0];

        MensajeEstado = "Tocá «Cargar datos» o escribí un nombre para buscar.";

        // Suscripción a los cambios de favoritos hechos en otras pantallas.
        // El mensajero guarda una referencia débil: si este ViewModel deja de
        // usarse, la suscripción no impide que se libere de memoria.
        mensajero.RegisterAll(this);

        _inicializado = true;
    }

    // --- Reacciones a cambios del usuario ---
    // El generador del Toolkit crea estos métodos parciales y los llama desde
    // el setter de cada propiedad. Es el punto de enganche para reaccionar a
    // un cambio sin escribir el setter completo a mano.

    partial void OnTextoBusquedaChanged(string value) => ProgramarBusqueda();

    partial void OnFiltroSeleccionadoChanged(OpcionFiltro? value) => ProgramarBusqueda();

    /// <summary>
    /// Mantiene sincronizada la estrella de la tarjeta cuando el favorito se
    /// cambia desde otra pantalla (por ejemplo, desde el detalle).
    /// </summary>
    public void Receive(FavoritoCambiadoMensaje mensaje)
    {
        foreach (var item in Personajes.Where(p => p.Id == mensaje.Personaje.Id))
        {
            item.EsFavorito = mensaje.EsFavorito;
        }
    }

    /// <summary>
    /// Implementa el "debounce": cancela la búsqueda que estuviera pendiente y
    /// programa una nueva tras un breve silencio del teclado.
    /// </summary>
    private void ProgramarBusqueda()
    {
        if (!_inicializado) return;

        _cancelacionBusqueda?.Cancel();
        _cancelacionBusqueda = new CancellationTokenSource();

        // Se descarta la tarea a propósito: el método maneja sus propias
        // excepciones y no hay nada que esperar desde el setter de la propiedad.
        _ = EsperarYBuscarAsync(_cancelacionBusqueda.Token);
    }

    private async Task EsperarYBuscarAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(MilisegundosDeEspera, ct);
            await BuscarDesdeCeroAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // El usuario siguió escribiendo: esta búsqueda ya no interesa.
        }
    }

    /// <summary>
    /// Carga la primera página con los filtros actuales, reemplazando la lista.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task CargarPersonajesAsync() => BuscarDesdeCeroAsync(CancellationToken.None);

    /// <summary>
    /// Vuelve a pedir la primera página. Enlazado al gesto de arrastrar hacia
    /// abajo (RefreshView).
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefrescarAsync()
    {
        EstaRefrescando = true;
        await BuscarDesdeCeroAsync(CancellationToken.None);
        EstaRefrescando = false;
    }

    private async Task BuscarDesdeCeroAsync(CancellationToken ct)
    {
        var version = ++_versionConsulta;
        EstaCargando = true;
        MostrarInfo("Buscando...");

        ResultadoApi<RespuestaPaginada> resultado;
        try
        {
            resultado = await _servicio.ObtenerPersonajesAsync(
                pagina: 1,
                nombre: TextoBusqueda,
                estado: FiltroSeleccionado?.ValorApi,
                ct);
        }
        catch (OperationCanceledException)
        {
            // Se canceló porque el usuario siguió escribiendo. No se toca el
            // indicador de carga: ahora le pertenece a la búsqueda nueva.
            return;
        }

        // Si mientras viajaba la respuesta empezó otra búsqueda, se descarta
        // el resultado: mostrarlo sobrescribiría la búsqueda más reciente.
        if (version != _versionConsulta)
        {
            return;
        }

        Personajes.Clear();

        if (resultado.EsExitoso && resultado.Datos is not null)
        {
            AgregarPersonajes(resultado.Datos.Resultados);

            _paginaActual = 1;
            _hayPaginaSiguiente = resultado.Datos.Info?.PaginaSiguiente is not null;

            InformarProgreso(resultado);
        }
        else
        {
            _paginaActual = 0;
            _hayPaginaSiguiente = false;
            OcultarAvisoCache();

            // SinResultados no es una falla: es una búsqueda que no encontró
            // nada. Se informa sin el formato de error para no alarmar.
            if (resultado.Error == TipoError.SinResultados)
            {
                MostrarInfo(resultado.Mensaje);
            }
            else
            {
                MostrarError(resultado.Mensaje);
            }
        }

        EstaCargando = false;
    }

    /// <summary>
    /// Agrega la página siguiente al final de la lista. Lo dispara el
    /// CollectionView cuando el usuario se acerca al final del scroll.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task CargarMasAsync()
    {
        // Tres guardas: que haya más páginas, que no haya otra carga en curso
        // y que ya se haya cargado algo. Sin ellas, el umbral del scroll
        // dispara pedidos repetidos de la misma página.
        if (!_hayPaginaSiguiente || EstaCargando || EstaCargandoMas || _paginaActual == 0)
        {
            return;
        }

        var version = _versionConsulta;
        EstaCargandoMas = true;

        var resultado = await _servicio.ObtenerPersonajesAsync(
            pagina: _paginaActual + 1,
            nombre: TextoBusqueda,
            estado: FiltroSeleccionado?.ValorApi);

        EstaCargandoMas = false;

        // Si mientras llegaba esta página empezó otra búsqueda, la lista ya
        // es otra: agregarle esta página mezclaría resultados de dos consultas.
        if (version != _versionConsulta)
        {
            return;
        }

        if (resultado.EsExitoso && resultado.Datos is not null)
        {
            AgregarPersonajes(resultado.Datos.Resultados);

            _paginaActual++;
            _hayPaginaSiguiente = resultado.Datos.Info?.PaginaSiguiente is not null;

            InformarProgreso(resultado);
        }
        else
        {
            MostrarError(resultado.Mensaje);
        }
    }

    /// <summary>
    /// Alterna el modo sin conexión simulado y recarga, para poder demostrar
    /// el manejo de la falta de red y la caché sin apagar el wifi.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task AlternarSimulacionAsync()
    {
        SimulandoSinConexion = !SimulandoSinConexion;
        _conectividad.SimularSinConexion = SimulandoSinConexion;
        await BuscarDesdeCeroAsync(CancellationToken.None);
    }

    /// <summary>
    /// Pide a propósito un id inexistente para demostrar que la aplicación
    /// distingue un 404 de un fallo de conexión.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ProbarError404Async()
    {
        EstaCargando = true;
        MostrarInfo("Pidiendo un personaje inexistente (id 9999)...");

        var resultado = await _servicio.ObtenerPersonajePorIdAsync(9999);

        if (resultado.EsExitoso)
        {
            MostrarInfo("Inesperado: el personaje existe.");
        }
        else
        {
            MostrarError(resultado.Mensaje);
        }

        EstaCargando = false;
    }

    private void AgregarPersonajes(IEnumerable<Personaje> personajes)
    {
        foreach (var personaje in personajes)
        {
            Personajes.Add(new PersonajeItemViewModel(personaje, _serviciosDeItem));
        }
    }

    /// <summary>
    /// Actualiza el mensaje de estado y la franja de aviso. Si los datos vienen
    /// de la caché, el total informado es el de aquella consulta guardada.
    /// </summary>
    private void InformarProgreso(ResultadoApi<RespuestaPaginada> resultado)
    {
        var total = resultado.Datos?.Info?.TotalElementos ?? Personajes.Count;

        if (resultado.DesdeCache && resultado.FechaCache is { } fecha)
        {
            MostrarAvisoCache(fecha);
            MostrarInfo($"Mostrando {Personajes.Count} de {total} personajes (copia guardada).");
        }
        else
        {
            OcultarAvisoCache();
            MostrarInfo($"Mostrando {Personajes.Count} de {total} personajes.");
        }
    }
}
