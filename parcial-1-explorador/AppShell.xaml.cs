using ExploradorPersonajes.Vistas;

namespace ExploradorPersonajes;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Registro de rutas para la navegación por URI. Permite llamar a
        // GoToAsync(nameof(DetallePage)) desde cualquier ViewModel sin que
        // éste tenga que conocer la jerarquía de páginas.
        Routing.RegisterRoute(nameof(DetallePage), typeof(DetallePage));
    }
}
