using System.Collections.ObjectModel;
using AgendaContactosMaui.Datos;
using AgendaContactosMaui.Modelos;
using AgendaContactosMaui.Vistas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgendaContactosMaui.ViewModels;

/// <summary>
/// ViewModel de la pantalla principal: lista (Paso 3b), búsqueda (Paso 3c)
/// y eliminación rápida deslizando un contacto (Extra).
/// </summary>
public partial class ContactosViewModel : ObservableObject
{
    private readonly AgendaDatabase _db;

    /// <summary>
    /// Cada búsqueda incrementa este número. Si mientras se consulta la base el
    /// usuario sigue escribiendo, el resultado viejo se descarta al llegar tarde.
    /// </summary>
    private int _versionBusqueda;

    /// <summary>ObservableCollection notifica altas y bajas: la lista se actualiza sola.</summary>
    public ObservableCollection<Contacto> Contactos { get; } = new();

    [ObservableProperty]
    public partial string? TextoBusqueda { get; set; }

    [ObservableProperty]
    public partial string Resumen { get; set; }

    public ContactosViewModel(AgendaDatabase db)
    {
        _db = db;
        Resumen = string.Empty;
    }

    /// <summary>Búsqueda en vivo: se vuelve a filtrar con cada letra escrita.</summary>
    partial void OnTextoBusquedaChanged(string? value) => _ = CargarAsync();

    [RelayCommand]
    public async Task CargarAsync()
    {
        var version = ++_versionBusqueda;
        var texto = TextoBusqueda?.Trim() ?? string.Empty;

        var lista = texto.Length == 0
            ? await _db.ObtenerTodosAsync()
            : await _db.BuscarPorNombreAsync(texto);

        if (version != _versionBusqueda)
            return;

        Contactos.Clear();
        foreach (var contacto in lista)
            Contactos.Add(contacto);

        ActualizarResumen();
    }

    [RelayCommand]
    private Task AgregarAsync() => Shell.Current.GoToAsync(nameof(ContactoPage));

    [RelayCommand]
    private async Task AbrirAsync(Contacto contacto)
    {
        Haptica.Vibrar();
        await Shell.Current.GoToAsync($"{nameof(ContactoPage)}?id={contacto.Id}");
    }

    /// <summary>Extra: elimina el contacto por su Id, previa confirmación.</summary>
    [RelayCommand]
    private async Task EliminarAsync(Contacto contacto)
    {
        var confirma = await Shell.Current.DisplayAlertAsync(
            "Eliminar contacto",
            $"¿Eliminar a {contacto.Nombre} (Id {contacto.Id})?",
            "Eliminar", "Cancelar");
        if (!confirma)
            return;

        await _db.EliminarAsync(contacto.Id);
        Haptica.Vibrar(HapticFeedbackType.LongPress);
        Contactos.Remove(contacto);
        ActualizarResumen();
    }

    private void ActualizarResumen()
    {
        var cantidad = Contactos.Count;
        var texto = TextoBusqueda?.Trim() ?? string.Empty;

        Resumen = texto.Length == 0
            ? (cantidad == 1 ? "1 contacto" : $"{cantidad} contactos")
            : (cantidad == 1 ? $"1 resultado para «{texto}»" : $"{cantidad} resultados para «{texto}»");
    }
}
