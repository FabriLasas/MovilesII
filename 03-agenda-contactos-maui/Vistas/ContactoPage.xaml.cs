using System.ComponentModel;
using AgendaContactosMaui.ViewModels;

namespace AgendaContactosMaui.Vistas;

public partial class ContactoPage : ContentPage
{
    public ContactoPage(ContactoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>Cuando aparece un mensaje de error, el texto "tiembla" para llamar la atención.</summary>
    private async void EtiquetaError_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Label.Text) || string.IsNullOrEmpty(EtiquetaError.Text))
            return;

        foreach (var desplazamiento in new[] { -8.0, 8, -5, 5, 0 })
            await EtiquetaError.TranslateToAsync(desplazamiento, 0, 45);
    }
}
