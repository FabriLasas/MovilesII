using AgendaContactosMaui.Datos;
using AgendaContactosMaui.ViewModels;
using AgendaContactosMaui.Vistas;
using Microsoft.Extensions.Logging;

namespace AgendaContactosMaui;

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

		// --- Datos ---
		// Singleton: una única conexión a SQLite compartida por toda la app.
		builder.Services.AddSingleton<AgendaDatabase>();

		// --- ViewModels y Vistas ---
		// Transient para el detalle: cada vez que se abre arranca limpio,
		// sin datos del contacto anterior.
		builder.Services.AddSingleton<ContactosViewModel>();
		builder.Services.AddSingleton<ContactosPage>();
		builder.Services.AddTransient<ContactoViewModel>();
		builder.Services.AddTransient<ContactoPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
