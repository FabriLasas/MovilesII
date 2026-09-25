using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace PerfilAlumno.Services;

/// <summary>
/// Implementacion con CommunityToolkit.Maui.
/// Requiere el paquete CommunityToolkit.Maui y .UseMauiCommunityToolkit()
/// en MauiProgram.cs (la plantilla con contenido de ejemplo ya lo trae).
/// </summary>
public class NotificationService : INotificationService
{
    public Task MostrarToastAsync(string mensaje) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // El Toast del toolkit todavia no funciona en Windows:
            // ahi mostramos un Snackbar para no perder el aviso.
            if (OperatingSystem.IsWindows())
            {
                await MostrarSnackbarAsync(mensaje);
                return;
            }

            var toast = Toast.Make(mensaje, ToastDuration.Short, 15);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await toast.Show(cts.Token);
        });

    public Task MostrarSnackbarAsync(string mensaje, string textoBoton = "OK", Action? alPresionar = null) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var opciones = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#512BD4"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(8),
                Font = Microsoft.Maui.Font.SystemFontOfSize(15),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14)
            };

            var snackbar = Snackbar.Make(
                mensaje,
                alPresionar,
                textoBoton,
                TimeSpan.FromSeconds(3),
                opciones);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            await snackbar.Show(cts.Token);
        });
}
