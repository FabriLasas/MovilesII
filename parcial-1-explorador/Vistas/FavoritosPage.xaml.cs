using ExploradorPersonajes.ViewModels;

namespace ExploradorPersonajes.Vistas;

public partial class FavoritosPage : ContentPage
{
    /// <summary>
    /// Igual que el resto de las vistas: sólo inicializa el XAML y recibe el
    /// ViewModel por inyección de dependencias.
    /// </summary>
    public FavoritosPage(FavoritosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
