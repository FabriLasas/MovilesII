using CommunityToolkit.Mvvm.ComponentModel;
using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pantalla de detalle.
///
/// Implementa IQueryAttributable, la interfaz que Shell invoca al completar la
/// navegación para entregar los parámetros recibidos. Se eligió por sobre el
/// atributo [QueryProperty] porque hace explícito en el código dónde y cómo se
/// recibe el parámetro, en lugar de depender de un mapeo por reflexión.
/// </summary>
public partial class DetalleViewModel : ViewModelBase, IQueryAttributable
{
    /// <summary>
    /// Clave del parámetro de navegación. Constante compartida para que el
    /// emisor y el receptor no puedan desincronizarse por un error de tipeo.
    /// </summary>
    public const string ClaveParametro = "personaje";

    [ObservableProperty]
    public partial Personaje? Personaje { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ClaveParametro, out var valor) && valor is Personaje seleccionado)
        {
            Personaje = seleccionado;
            Titulo = seleccionado.Nombre;
            MostrarInfo($"Detalle de {seleccionado.Nombre}");
        }
        else
        {
            MostrarError("No se recibió el personaje a mostrar.");
        }
    }
}
