using AgendaContactosMaui.Vistas;

namespace AgendaContactosMaui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Permite navegar con GoToAsync(nameof(ContactoPage)) desde los ViewModels.
		Routing.RegisterRoute(nameof(ContactoPage), typeof(ContactoPage));
	}
}
