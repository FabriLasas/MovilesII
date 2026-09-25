using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace PerfilAlumno;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Registro de las rutas de navegacion que no estan en AppShell.xaml.
        PerfilAlumno.Navigation.Routes.Registrar();

        return builder.Build();
    }
}
