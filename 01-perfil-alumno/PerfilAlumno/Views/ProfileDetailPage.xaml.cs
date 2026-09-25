using PerfilAlumno.ViewModels;

namespace PerfilAlumno.Views;

/// <summary>
/// Pagina de destino. Shell la crea al navegar a la ruta "detalle"
/// y le entrega los parametros a traves de IQueryAttributable del ViewModel.
/// </summary>
public partial class ProfileDetailPage : ContentPage
{
    public ProfileDetailPage()
    {
        InitializeComponent();
        BindingContext = new ProfileDetailViewModel();
    }
}
