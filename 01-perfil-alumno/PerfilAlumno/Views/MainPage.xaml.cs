using PerfilAlumno.ViewModels;

namespace PerfilAlumno.Views;

/// <summary>
/// VIEW: no contiene logica de negocio NI de navegacion.
/// Solo asigna el ViewModel como BindingContext.
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new ProfileViewModel();
    }
}
