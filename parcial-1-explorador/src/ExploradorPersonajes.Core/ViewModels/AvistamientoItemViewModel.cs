using CommunityToolkit.Mvvm.Input;
using ExploradorPersonajes.Datos;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// Un avistamiento en la lista del detalle. Sigue el mismo criterio que
/// <see cref="PersonajeItemViewModel"/>: cada elemento expone su propia
/// acción, así la plantilla de la lista no necesita alcanzar el ViewModel de
/// la pantalla.
/// </summary>
public partial class AvistamientoItemViewModel
{
    private readonly Func<AvistamientoItemViewModel, Task> _eliminar;

    public AvistamientoItemViewModel(Avistamiento modelo, Func<AvistamientoItemViewModel, Task> eliminar)
    {
        Modelo = modelo;
        _eliminar = eliminar;
    }

    public Avistamiento Modelo { get; }

    public string RutaFoto => Modelo.RutaFoto;
    public string Nota => Modelo.Nota;
    public string TextoFecha => Modelo.TextoFecha;
    public string TextoUbicacion => Modelo.TieneUbicacion ? $"📍 {Modelo.TextoUbicacion}" : Modelo.TextoUbicacion;

    [RelayCommand]
    private Task EliminarAsync() => _eliminar(this);
}
