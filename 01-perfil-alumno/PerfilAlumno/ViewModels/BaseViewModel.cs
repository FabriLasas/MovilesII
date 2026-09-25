using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PerfilAlumno.ViewModels;

/// <summary>
/// Clase base con la plomeria de INotifyPropertyChanged,
/// para no repetirla en cada ViewModel.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Asigna solo si cambio y notifica. Devuelve true si hubo cambio.</summary>
    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;
        OnPropertyChanged(propertyName);
        return true;
    }
}
