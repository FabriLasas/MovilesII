namespace PerfilAlumno.Services;

/// <summary>
/// Notificaciones visuales (toast / snackbar) desacopladas del ViewModel.
/// </summary>
public interface INotificationService
{
    /// <summary>Mensaje breve y no intrusivo. Ideal para confirmaciones.</summary>
    Task MostrarToastAsync(string mensaje);

    /// <summary>Mensaje con boton de accion. Ideal para errores.</summary>
    Task MostrarSnackbarAsync(string mensaje, string textoBoton = "OK", Action? alPresionar = null);
}
