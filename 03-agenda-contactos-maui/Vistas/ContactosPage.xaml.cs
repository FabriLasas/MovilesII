using AgendaContactosMaui.ViewModels;

namespace AgendaContactosMaui.Vistas;

public partial class ContactosPage : ContentPage
{
    private readonly ContactosViewModel _viewModel;

    public ContactosPage(ContactosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>
    /// Se recarga cada vez que la página vuelve a verse, así aparecen los
    /// cambios hechos en la pantalla de detalle (alta, edición o baja).
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarCommand.ExecuteAsync(null);

        // La primera vez, la lista aparece con un fundido suave en vez de "saltar".
        if (Lista.Opacity < 1)
            await Lista.FadeToAsync(1, 250, Easing.CubicOut);
    }

    // Micro-animación del botón flotante: se achica al presionarlo y rebota al soltarlo.
    private async void BotonAgregar_Pressed(object? sender, EventArgs e) =>
        await BotonAgregar.ScaleToAsync(0.9, 80, Easing.CubicOut);

    private async void BotonAgregar_Released(object? sender, EventArgs e) =>
        await BotonAgregar.ScaleToAsync(1, 180, Easing.SpringOut);
}
