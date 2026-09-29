using CommunityToolkit.Mvvm.Messaging;
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

        // --- APIs de la plataforma ---
        // Se registran sus interfaces en lugar de usar las clases estáticas
        // (Preferences.Default, Connectivity.Current...) dentro del código.
        // Así cada servicio declara de qué depende y puede probarse con dobles.
        builder.Services.AddSingleton(Connectivity.Current);
        builder.Services.AddSingleton(FileSystem.Current);
        builder.Services.AddSingleton(Preferences.Default);
        builder.Services.AddSingleton(HapticFeedback.Default);

        // Mensajero con referencias débiles: una pantalla suscrita que deja de
        // usarse puede liberarse de memoria aunque no se desuscriba.
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // --- Servicios propios ---
        // Singleton: guardan estado que tiene que ser uno solo en toda la app
        // (la lista de favoritos, el modo sin conexión simulado).
        builder.Services.AddSingleton<IServicioConectividad, ServicioConectividad>();
        builder.Services.AddSingleton<IServicioCache, ServicioCache>();
        builder.Services.AddSingleton<IServicioFavoritos, ServicioFavoritos>();
        builder.Services.AddSingleton<IServicioNavegacion, ServicioNavegacion>();
        builder.Services.AddSingleton<ServiciosDeItem>();

        // AddHttpClient registra una fábrica que reutiliza los sockets entre
        // llamadas. Crear un HttpClient nuevo por request agota los puertos
        // disponibles bajo uso intensivo.
        builder.Services.AddHttpClient<IServicioPersonajes, ServicioPersonajes>();

        // --- ViewModels y Vistas ---
        // Singleton para las pestañas: conservan la búsqueda y el scroll al
        // ir y volver entre ellas.
        builder.Services.AddSingleton<PersonajesViewModel>();
        builder.Services.AddSingleton<PersonajesPage>();
        builder.Services.AddSingleton<FavoritosViewModel>();
        builder.Services.AddSingleton<FavoritosPage>();

        // Transient: cada navegación al detalle crea una instancia nueva, para
        // que no queden datos del personaje anterior al abrir otro.
        builder.Services.AddTransient<DetalleViewModel>();
        builder.Services.AddTransient<DetallePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
