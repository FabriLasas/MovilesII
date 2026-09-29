using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

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

    private readonly IServicioFavoritos _favoritos;
    private readonly IHapticFeedback _haptica;

    public DetalleViewModel(IServicioFavoritos favoritos, IHapticFeedback haptica)
    {
        _favoritos = favoritos;
        _haptica = haptica;
    }

    [ObservableProperty]
    public partial Personaje? Personaje { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoBotonFavorito))]
    public partial bool EsFavorito { get; set; }

    public string TextoBotonFavorito => EsFavorito ? "★  Quitar de favoritos" : "☆  Agregar a favoritos";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ClaveParametro, out var valor) && valor is Personaje seleccionado)
        {
            Personaje = seleccionado;
            Titulo = seleccionado.Nombre;
            EsFavorito = _favoritos.EsFavorito(seleccionado.Id);
            MostrarInfo($"Detalle de {seleccionado.Nombre}");
        }
        else
        {
            MostrarError("No se recibió el personaje a mostrar.");
        }
    }

    /// <summary>
    /// El servicio publica el cambio, así que al volver a la lista la estrella
    /// de la tarjeta ya aparece actualizada sin recargar nada.
    /// </summary>
    [RelayCommand]
    private void AlternarFavorito()
    {
        if (Personaje is null) return;

        EsFavorito = _favoritos.Alternar(Personaje);
        _haptica.VibrarSiEsPosible();
    }
}
