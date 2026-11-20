using ExploradorPersonajes.ViewModels;

namespace ExploradorPersonajes.Vistas;

public partial class NuevoAvistamientoPage : ContentPage
{
    public NuevoAvistamientoPage(NuevoAvistamientoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
