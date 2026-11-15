using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pantalla de detalle.
///
/// Recibe el personaje con <see cref="Recibir"/>, que toma los parámetros de
/// navegación como un diccionario común. No implementa IQueryAttributable
/// porque esa interfaz es de MAUI y el núcleo no depende de la plataforma: la
/// aplicación la agrega en una subclase que sólo delega en este método.
/// </summary>
public partial class DetalleViewModel : ViewModelBase
{
    /// <summary>
    /// Clave del parámetro de navegación. Constante compartida para que el
    /// emisor y el receptor no puedan desincronizarse por un error de tipeo.
    /// </summary>
    public const string ClaveParametro = "personaje";

    private readonly IServicioFavoritos _favoritos;
    private readonly IServicioHaptica _haptica;

    public DetalleViewModel(IServicioFavoritos favoritos, IServicioHaptica haptica)
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

    /// <summary>
    /// Recibe los parámetros de navegación. Valida que llegue un personaje en
    /// lugar de suponerlo: si la navegación se armó mal, se informa en
    /// pantalla en vez de fallar con una excepción.
    /// </summary>
    public void Recibir(IDictionary<string, object> parametros)
    {
        if (parametros.TryGetValue(ClaveParametro, out var valor) && valor is Personaje seleccionado)
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
        _haptica.ConfirmarToque();
    }
}
