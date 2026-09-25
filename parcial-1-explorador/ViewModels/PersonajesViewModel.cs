using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.Vistas;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pantalla principal. Concentra el estado y las acciones de
/// la lista de personajes: carga, búsqueda, filtrado y paginación.
/// No conoce ningún control de la interfaz: se comunica con la vista sólo a
/// través de propiedades enlazadas y comandos.
/// </summary>
public partial class PersonajesViewModel : ViewModelBase
{
    /// <summary>
    /// Espera desde la última tecla antes de consultar a la API. Sin esta
    /// demora, escribir "rick" dispararía cuatro llamadas de red en lugar de
    /// una: tres inútiles y una correcta.
    /// </summary>
    private const int MilisegundosDeEspera = 500;

    private readonly IServicioPersonajes _servicio;

    /// <summary>
    /// Permite cancelar la búsqueda pendiente cuando el usuario sigue
    /// escribiendo, y también la llamada en curso si ya había salido.
    /// </summary>
    private CancellationTokenSource? _cancelacionBusqueda;

    private int _paginaActual;
    private bool _hayPaginaSiguiente;

    /// <summary>
    /// Evita que las asignaciones del constructor disparen una búsqueda antes
    /// de que la pantalla esté lista.
    /// </summary>
    private readonly bool _inicializado;

    /// <summary>
    /// ObservableCollection (y no List) porque notifica altas y bajas a la
    /// interfaz: el CollectionView se actualiza solo al agregar elementos.
    /// </summary>
    public ObservableCollection<Personaje> Personajes { get; } = new();

    public IReadOnlyList<OpcionFiltro> OpcionesFiltro { get; }

    [ObservableProperty]
    public partial string TextoBusqueda { get; set; }

    [ObservableProperty]
    public partial OpcionFiltro? FiltroSeleccionado { get; set; }

    [ObservableProperty]
    public partial bool EstaRefrescando { get; set; }

    [ObservableProperty]
    public partial bool EstaCargandoMas { get; set; }

    public bool ListaVacia => Personajes.Count == 0;

    public PersonajesViewModel(IServicioPersonajes servicio)
    {
        _servicio = servicio;
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
        _inicializado = true;
    }

    // --- Reacciones a cambios del usuario ---
    // El generador del Toolkit crea estos métodos parciales y los llama desde
    // el setter de cada propiedad. Es el punto de enganche para reaccionar a
    // un cambio sin escribir el setter completo a mano.

    partial void OnTextoBusquedaChanged(string value) => ProgramarBusqueda();

    partial void OnFiltroSeleccionadoChanged(OpcionFiltro? value) => ProgramarBusqueda();

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
        EstaCargando = true;
        MostrarInfo("Buscando...");

        var resultado = await _servicio.ObtenerPersonajesAsync(
            pagina: 1,
            nombre: TextoBusqueda,
            estado: FiltroSeleccionado?.ValorApi,
            ct);

        // Si mientras viajaba la respuesta el usuario cambió la búsqueda, se
        // descarta el resultado: mostrarlo sobrescribiría la búsqueda nueva.
        if (ct.IsCancellationRequested)
        {
            EstaCargando = false;
            return;
        }

        Personajes.Clear();

        if (resultado.EsExitoso && resultado.Datos is not null)
        {
            foreach (var personaje in resultado.Datos.Resultados)
            {
                Personajes.Add(personaje);
            }

            _paginaActual = 1;
            _hayPaginaSiguiente = resultado.Datos.Info?.PaginaSiguiente is not null;

            var total = resultado.Datos.Info?.TotalElementos ?? Personajes.Count;
            MostrarInfo($"Mostrando {Personajes.Count} de {total} personajes.");
        }
        else
        {
            _paginaActual = 0;
            _hayPaginaSiguiente = false;

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

        OnPropertyChanged(nameof(ListaVacia));
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

        EstaCargandoMas = true;

        var resultado = await _servicio.ObtenerPersonajesAsync(
            pagina: _paginaActual + 1,
            nombre: TextoBusqueda,
            estado: FiltroSeleccionado?.ValorApi);

        if (resultado.EsExitoso && resultado.Datos is not null)
        {
            foreach (var personaje in resultado.Datos.Resultados)
            {
                Personajes.Add(personaje);
            }

            _paginaActual++;
            _hayPaginaSiguiente = resultado.Datos.Info?.PaginaSiguiente is not null;

            var total = resultado.Datos.Info?.TotalElementos ?? Personajes.Count;
            MostrarInfo($"Mostrando {Personajes.Count} de {total} personajes.");
        }
        else
        {
            MostrarError(resultado.Mensaje);
        }

        EstaCargandoMas = false;
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

    /// <summary>
    /// Navega al detalle pasando el personaje seleccionado como parámetro.
    /// La navegación se dispara desde el ViewModel, no desde el code-behind.
    /// </summary>
    [RelayCommand]
    private async Task VerDetalleAsync(Personaje? personaje)
    {
        if (personaje is null) return;

        await Shell.Current.GoToAsync(nameof(DetallePage), new Dictionary<string, object>
        {
            [DetalleViewModel.ClaveParametro] = personaje
        });
    }
}
