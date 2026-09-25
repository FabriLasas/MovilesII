using System.Windows.Input;
using PerfilAlumno.Navigation;
using PerfilAlumno.Services;

namespace PerfilAlumno.ViewModels;

/// <summary>
/// ViewModel de la pagina de detalle. Recibe los parametros que envio
/// MainPage y los VALIDA antes de mostrarlos.
///
/// IQueryAttributable es el mecanismo de Shell para entregar los
/// parametros de GoToAsync: se ejecuta automaticamente al navegar,
/// antes de que la pagina se muestre.
/// </summary>
public class ProfileDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly INavigationService _navegacion;
    private readonly INotificationService _notificaciones;

    public ProfileDetailViewModel(
        INavigationService? navegacion = null,
        INotificationService? notificaciones = null)
    {
        _navegacion = navegacion ?? new NavigationService();
        _notificaciones = notificaciones ?? new NotificationService();

        VolverCommand = new Command(async () => await VolverAsync());
    }

    // ---------------- Datos recibidos ----------------

    private string _nombre = string.Empty;
    public string Nombre { get => _nombre; private set => SetProperty(ref _nombre, value); }

    private int _edad;
    public int Edad
    {
        get => _edad;
        private set { if (SetProperty(ref _edad, value)) OnPropertyChanged(nameof(EdadTexto)); }
    }

    public string EdadTexto => $"{Edad} anios";

    private string _descripcion = string.Empty;
    public string Descripcion { get => _descripcion; private set => SetProperty(ref _descripcion, value); }

    private string _imagenUrl = "dotnet_bot.png";
    public string ImagenUrl { get => _imagenUrl; private set => SetProperty(ref _imagenUrl, value); }

    // ---------------- Estado de la validacion ----------------

    private bool _datosValidos;
    public bool DatosValidos
    {
        get => _datosValidos;
        private set { if (SetProperty(ref _datosValidos, value)) OnPropertyChanged(nameof(HayProblema)); }
    }

    public bool HayProblema => !DatosValidos;

    private string _mensajeError = string.Empty;
    public string MensajeError { get => _mensajeError; private set => SetProperty(ref _mensajeError, value); }

    public ICommand VolverCommand { get; }

    /// <summary>
    /// Punto de entrada de los parametros de navegacion.
    /// Nunca confiamos en que lleguen: validamos presencia, tipo y rango.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var errores = new List<string>();

        // --- Nombre: obligatorio y no vacio ---
        if (!query.TryGetValue(ParametrosPerfil.Nombre, out var valorNombre)
            || valorNombre is not string nombre
            || string.IsNullOrWhiteSpace(nombre))
        {
            errores.Add("no llego un nombre valido");
        }
        else
        {
            Nombre = nombre;
        }

        // --- Edad: obligatoria, numerica y en rango ---
        if (!query.TryGetValue(ParametrosPerfil.Edad, out var valorEdad)
            || !int.TryParse(valorEdad?.ToString(), out int edad)
            || edad <= 0 || edad >= 120)
        {
            errores.Add("la edad es invalida");
        }
        else
        {
            Edad = edad;
        }

        // --- Descripcion e imagen: opcionales, con valor por defecto ---
        Descripcion = query.TryGetValue(ParametrosPerfil.Descripcion, out var d) && d is string desc && !string.IsNullOrWhiteSpace(desc)
            ? desc
            : "(sin descripcion)";

        if (query.TryGetValue(ParametrosPerfil.Imagen, out var i) && i is string img && !string.IsNullOrWhiteSpace(img))
            ImagenUrl = img;

        // --- Resultado de la validacion ---
        DatosValidos = errores.Count == 0;

        if (DatosValidos)
        {
            MensajeError = string.Empty;
            _ = _notificaciones.MostrarToastAsync($"Perfil de {Nombre} cargado.");
        }
        else
        {
            MensajeError = "No se pudo mostrar el perfil: " + string.Join(", ", errores) + ".";
            // Snackbar con accion: le damos al usuario una salida.
            _ = _notificaciones.MostrarSnackbarAsync(
                MensajeError,
                "Volver",
                async () => await _navegacion.VolverAsync());
        }
    }

    private async Task VolverAsync()
    {
        await _navegacion.VolverAsync();
        await _notificaciones.MostrarToastAsync("Volviste al perfil.");
    }
}
