using CommunityToolkit.Mvvm.ComponentModel;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// Base común de los ViewModels. Hereda de ObservableObject (Toolkit), que ya
/// implementa INotifyPropertyChanged, de modo que no hay que escribir a mano
/// el evento ni los setters con notificación.
///
/// Las propiedades se declaran como "partial": el generador de código del
/// Toolkit completa la implementación con la notificación de cambios.
/// </summary>
public partial class ViewModelBase : ObservableObject
{
    /// <summary>Indica que hay una operación en curso: controla el spinner.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoEstaCargando))]
    public partial bool EstaCargando { get; set; }

    /// <summary>Mensaje de estado que se muestra al usuario.</summary>
    [ObservableProperty]
    public partial string MensajeEstado { get; set; }

    /// <summary>Distingue un mensaje de error de uno informativo para el color.</summary>
    [ObservableProperty]
    public partial bool HayError { get; set; }

    [ObservableProperty]
    public partial string Titulo { get; set; }

    /// <summary>Negación expuesta como propiedad para poder enlazarla desde XAML.</summary>
    public bool NoEstaCargando => !EstaCargando;

    protected ViewModelBase()
    {
        MensajeEstado = string.Empty;
        Titulo = string.Empty;
    }

    protected void MostrarInfo(string mensaje)
    {
        HayError = false;
        MensajeEstado = mensaje;
    }

    protected void MostrarError(string mensaje)
    {
        HayError = true;
        MensajeEstado = mensaje;
    }
}
