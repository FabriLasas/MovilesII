using AgendaContactosMaui.Datos;
using AgendaContactosMaui.Modelos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgendaContactosMaui.ViewModels;

/// <summary>
/// ViewModel del formulario de un contacto. Sirve para dos casos:
///  - Alta: se abre sin parámetros y todos los campos son editables.
///  - Detalle / edición: se abre con ?id=N, muestra todos los datos y permite
///    actualizar teléfono o email (Extra) o eliminar el contacto (Extra).
///
/// Implementa IQueryAttributable: Shell llama a ApplyQueryAttributes con los
/// parámetros de la ruta al navegar a la página.
/// </summary>
public partial class ContactoViewModel : ObservableObject, IQueryAttributable
{
    private readonly AgendaDatabase _db;

    public int MaxNombre => AgendaDatabase.MaxNombre;

    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Nombre { get; set; }

    [ObservableProperty]
    public partial string Telefono { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsExistente))]
    public partial bool EsNuevo { get; set; }

    public bool EsExistente => !EsNuevo;

    [ObservableProperty]
    public partial string Titulo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayError))]
    public partial string MensajeError { get; set; }

    public bool HayError => MensajeError.Length > 0;

    public ContactoViewModel(AgendaDatabase db)
    {
        _db = db;
        Nombre = string.Empty;
        Telefono = string.Empty;
        Email = string.Empty;
        EsNuevo = true;
        Titulo = "Nuevo contacto";
        MensajeError = string.Empty;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valor) && int.TryParse(valor?.ToString(), out var id))
            _ = CargarAsync(id);
    }

    private async Task CargarAsync(int id)
    {
        var contacto = await _db.ObtenerPorIdAsync(id);
        if (contacto is null)
        {
            MensajeError = $"No existe un contacto con Id {id}.";
            return;
        }

        Id = contacto.Id;
        Nombre = contacto.Nombre;
        Telefono = contacto.Telefono;
        Email = contacto.Email;
        EsNuevo = false;
        Titulo = contacto.Nombre;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;

        var contacto = new Contacto
        {
            Id = Id,
            Nombre = Nombre.Trim(),
            Telefono = Telefono.Trim(),
            Email = Email.Trim()
        };

        try
        {
            if (EsNuevo)
                await _db.InsertarAsync(contacto);
            else
                await _db.ActualizarAsync(contacto);
        }
        catch (ArgumentException ex)
        {
            MensajeError = ex.Message;
            Haptica.Vibrar(HapticFeedbackType.LongPress);
            return;
        }

        Haptica.Vibrar();
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Extra: elimina el contacto por su Id.</summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task EliminarAsync()
    {
        var confirma = await Shell.Current.DisplayAlertAsync(
            "Eliminar contacto",
            $"¿Eliminar a {Nombre} (Id {Id})?",
            "Eliminar", "Cancelar");
        if (!confirma)
            return;

        await _db.EliminarAsync(Id);
        Haptica.Vibrar(HapticFeedbackType.LongPress);
        await Shell.Current.GoToAsync("..");
    }
}
