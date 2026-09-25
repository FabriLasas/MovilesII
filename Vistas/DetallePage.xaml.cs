using ExploradorPersonajes.ViewModels;

namespace ExploradorPersonajes.Vistas;

public partial class DetallePage : ContentPage
{
    /// <summary>
    /// El BindingContext se asigna en el constructor, antes de que Shell
    /// entregue los parámetros de navegación. Eso permite que Shell encuentre
    /// el ViewModel y le pase el personaje seleccionado.
    /// </summary>
    public DetallePage(DetalleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
