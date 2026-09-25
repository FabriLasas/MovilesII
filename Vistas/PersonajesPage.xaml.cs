using ExploradorPersonajes.ViewModels;

namespace ExploradorPersonajes.Vistas;

public partial class PersonajesPage : ContentPage
{
    /// <summary>
    /// El code-behind se limita a inicializar el XAML y a recibir el ViewModel
    /// por inyección de dependencias. Toda la lógica vive en el ViewModel.
    /// </summary>
    public PersonajesPage(PersonajesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
