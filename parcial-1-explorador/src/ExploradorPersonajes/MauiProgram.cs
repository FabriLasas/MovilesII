using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Plataforma;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.ViewModels;
using ExploradorPersonajes.Vistas;
using Microsoft.Extensions.Logging;
using SQLite;

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
        builder.Services.AddSingleton(Preferences.Default);
        builder.Services.AddSingleton(HapticFeedback.Default);
        builder.Services.AddSingleton(Vibration.Default);
        builder.Services.AddSingleton(MediaPicker.Default);
        builder.Services.AddSingleton(Geolocation.Default);
        builder.Services.AddSingleton(AppInfo.Current);
        builder.Services.AddSingleton(Accelerometer.Default);

        // Mensajero con referencias débiles: una pantalla suscrita que deja de
        // usarse puede liberarse de memoria aunque no se desuscriba.
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // --- Servicios propios ---
        // Las interfaces viven en el núcleo (ExploradorPersonajes.Core) y las
        // implementaciones que dependen del dispositivo, en la carpeta
        // Plataforma de la app. Este es el único lugar donde se unen.
        // Singleton: guardan estado que tiene que ser uno solo en toda la app
        // (la lista de favoritos, el modo sin conexión simulado).
        builder.Services.AddSingleton<IServicioConectividad, ServicioConectividad>();
        builder.Services.AddSingleton<IServicioCache>(_ => new ServicioCache(FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<IServicioFavoritos, ServicioFavoritos>();
        builder.Services.AddSingleton<IFavoritosAnteriores, FavoritosEnPreferencias>();
        builder.Services.AddSingleton<IServicioNavegacion, ServicioNavegacion>();
        builder.Services.AddSingleton<IServicioHaptica, ServicioHaptica>();
        builder.Services.AddSingleton<ServiciosDeItem>();

        // --- Hardware y permisos ---
        builder.Services.AddSingleton<IServicioPermisos, ServicioPermisos>();
        builder.Services.AddSingleton<IServicioUbicacion, ServicioUbicacion>();
        builder.Services.AddSingleton<IServicioMovimiento, ServicioMovimiento>();
        builder.Services.AddSingleton<IServicioFotos>(sp =>
            new ServicioFotos(sp.GetRequiredService<IMediaPicker>(), FileSystem.CacheDirectory, FileSystem.AppDataDirectory));

        // --- Base de datos local ---
        // Una sola conexión para toda la app, sobre un archivo en la carpeta
        // de datos (se borra sólo al desinstalar). SharedCache permite que
        // varios repositorios usen la misma base sin bloquearse.
        builder.Services.AddSingleton(_ => new SQLiteAsyncConnection(
            Path.Combine(FileSystem.AppDataDirectory, "explorador.db3"),
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache));
        builder.Services.AddSingleton<IRepositorioFavoritos>(sp =>
            new RepositorioFavoritosSqlite(sp.GetRequiredService<SQLiteAsyncConnection>()));
        builder.Services.AddSingleton<IRepositorioAvistamientos>(sp =>
            new RepositorioAvistamientosSqlite(sp.GetRequiredService<SQLiteAsyncConnection>()));

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

        // Transient: cada navegación crea una instancia nueva, para que no
        // queden datos del personaje anterior ni un formulario a medio llenar.
        // Se pide el ViewModel del núcleo pero se entrega el adaptador de
        // Shell, que agrega la recepción de parámetros de navegación.
        builder.Services.AddTransient<DetalleViewModel, DetalleViewModelShell>();
        builder.Services.AddTransient<DetallePage>();
        builder.Services.AddTransient<NuevoAvistamientoViewModel, NuevoAvistamientoViewModelShell>();
        builder.Services.AddTransient<NuevoAvistamientoPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
