using System.Windows.Input;
using PerfilAlumno.Models;
using PerfilAlumno.Navigation;
using PerfilAlumno.Services;

namespace PerfilAlumno.ViewModels;

/// <summary>
/// ViewModel de la pagina principal: edicion del perfil + origen de la navegacion.
///
/// FLUJO DE NAVEGACION (ver NAVIGATION.md):
///   MainPage --[VerDetalleCommand]--> "detalle" (ProfileDetailPage)
///   Los datos viajan como diccionario de parametros, nunca como estado global.
/// </summary>
public class ProfileViewModel : BaseViewModel
{
    private readonly UserProfile _perfil;
    private readonly INavigationService _navegacion;
    private readonly INotificationService _notificaciones;

    // Los parametros tienen valor por defecto para que la pagina pueda
    // instanciar el ViewModel sin contenedor de dependencias, pero en un
    // test se pueden inyectar dobles de prueba.
    public ProfileViewModel(
        INavigationService? navegacion = null,
        INotificationService? notificaciones = null)
    {
        _navegacion = navegacion ?? new NavigationService();
        _notificaciones = notificaciones ?? new NotificationService();

        _perfil = new UserProfile
        {
            Nombre = "Juan Perez",
            Edad = 22,
            Descripcion = "Estudiante de Desarrollo de Software.",
            ImagenUrl = "dotnet_bot.png"
        };

        _nombre = _perfil.Nombre;
        _edadTexto = _perfil.Edad.ToString();
        _descripcion = _perfil.Descripcion;

        GuardarCommand = new Command(async () => await GuardarAsync(), PuedeGuardar);
        VerDetalleCommand = new Command(async () => await VerDetalleAsync());
    }

    // ---------------- Campos editables (binding TwoWay) ----------------

    private string _nombre;
    public string Nombre
    {
        get => _nombre;
        set { if (SetProperty(ref _nombre, value)) RefrescarEstado(); }
    }

    private string _edadTexto;
    public string EdadTexto
    {
        get => _edadTexto;
        set { if (SetProperty(ref _edadTexto, value)) RefrescarEstado(); }
    }

    private string _descripcion;
    public string Descripcion
    {
        get => _descripcion;
        set { if (SetProperty(ref _descripcion, value)) RefrescarEstado(); }
    }

    // ---------------- Datos ya guardados (solo lectura) ----------------

    public string NombreGuardado => _perfil.Nombre;
    public string EdadGuardada => $"{_perfil.Edad} anios";
    public string DescripcionGuardada => _perfil.Descripcion;
    public string ImagenUrl => _perfil.ImagenUrl;

    // ---------------- Mensajes ----------------

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }

    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True si el usuario edito algo y todavia no lo guardo.</summary>
    public bool HayCambiosSinGuardar =>
        Nombre != _perfil.Nombre
        || EdadTexto != _perfil.Edad.ToString()
        || Descripcion != _perfil.Descripcion;

    // ---------------- Commands ----------------

    public ICommand GuardarCommand { get; }
    public ICommand VerDetalleCommand { get; }

    private bool PuedeGuardar()
        => !string.IsNullOrWhiteSpace(Nombre)
           && int.TryParse(EdadTexto, out int edad)
           && edad > 0 && edad < 120;

    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            MensajeError = "El nombre no puede estar vacio.";
            await _notificaciones.MostrarSnackbarAsync(MensajeError);
            return;
        }

        if (!int.TryParse(EdadTexto, out int edad) || edad <= 0 || edad >= 120)
        {
            MensajeError = "Ingresa una edad valida (entre 1 y 119).";
            await _notificaciones.MostrarSnackbarAsync(MensajeError);
            return;
        }

        _perfil.Nombre = Nombre.Trim();
        _perfil.Edad = edad;
        _perfil.Descripcion = Descripcion?.Trim() ?? string.Empty;

        MensajeError = string.Empty;

        OnPropertyChanged(nameof(NombreGuardado));
        OnPropertyChanged(nameof(EdadGuardada));
        OnPropertyChanged(nameof(DescripcionGuardada));
        OnPropertyChanged(nameof(HayCambiosSinGuardar));

        // Notificacion visual: confirmacion breve de una accion exitosa.
        await _notificaciones.MostrarToastAsync("Perfil actualizado correctamente.");
    }

    /// <summary>
    /// Navega al detalle. Toda la decision (validar, armar parametros,
    /// avisar al usuario) vive aca, en el ViewModel. La View solo bindea.
    /// </summary>
    private async Task VerDetalleAsync()
    {
        // Validacion previa: no navegamos con datos a medio editar.
        if (HayCambiosSinGuardar)
        {
            await _notificaciones.MostrarSnackbarAsync(
                "Tenes cambios sin guardar. Guardalos antes de ver el detalle.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_perfil.Nombre) || _perfil.Edad <= 0)
        {
            await _notificaciones.MostrarSnackbarAsync("El perfil esta incompleto.");
            return;
        }

        // Los parametros viajan tipados dentro del diccionario.
        var parametros = new Dictionary<string, object>
        {
            [ParametrosPerfil.Nombre] = _perfil.Nombre,
            [ParametrosPerfil.Edad] = _perfil.Edad,
            [ParametrosPerfil.Descripcion] = _perfil.Descripcion,
            [ParametrosPerfil.Imagen] = _perfil.ImagenUrl
        };

        bool navego = await _navegacion.IrAAsync(Routes.Detalle, parametros);

        if (!navego)
            await _notificaciones.MostrarSnackbarAsync($"Error de navegacion: {_navegacion.UltimoError}");
    }

    private void RefrescarEstado()
    {
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayCambiosSinGuardar));
        ((Command)GuardarCommand).ChangeCanExecute();
    }
}
