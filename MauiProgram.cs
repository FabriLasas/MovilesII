using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.ViewModels;
using ExploradorPersonajes.Vistas;
using Microsoft.Extensions.Logging;

namespace ExploradorPersonajes;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // --- Servicios ---
        // AddHttpClient registra una fábrica que reutiliza los sockets entre
        // llamadas. Crear un HttpClient nuevo por request agota los puertos
        // disponibles bajo uso intensivo.
        builder.Services.AddHttpClient<IServicioPersonajes, ServicioPersonajes>();

        // --- ViewModels y Vistas ---
        // Transient: cada navegación al detalle crea una instancia nueva, para
        // que no queden datos del personaje anterior al abrir otro.
        builder.Services.AddSingleton<PersonajesViewModel>();
        builder.Services.AddSingleton<PersonajesPage>();
        builder.Services.AddTransient<DetalleViewModel>();
        builder.Services.AddTransient<DetallePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
