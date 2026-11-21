using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// Servicios que necesita cada tarjeta de personaje, agrupados en un solo
/// objeto. Evita que las pantallas que crean tarjetas tengan que recibir y
/// pasar tres dependencias sueltas cada vez.
/// </summary>
public class ServiciosDeItem
{
    public ServiciosDeItem(IServicioFavoritos favoritos, IServicioNavegacion navegacion, IServicioHaptica haptica)
    {
        Favoritos = favoritos;
        Navegacion = navegacion;
        Haptica = haptica;
    }

    public IServicioFavoritos Favoritos { get; }
    public IServicioNavegacion Navegacion { get; }
    public IServicioHaptica Haptica { get; }
}

/// <summary>
/// ViewModel de una tarjeta de la lista.
///
/// Existe porque "es favorito" es estado de la interfaz, no un dato de la API:
/// ponerlo dentro del modelo Personaje mezclaría ambas cosas. El modelo queda
/// como datos puros y este ViewModel lo envuelve, suma el estado propio de la
/// pantalla y expone las acciones de la tarjeta (marcar y abrir el detalle).
/// </summary>
public partial class PersonajeItemViewModel : ObservableObject
{
    private readonly ServiciosDeItem _servicios;

    public PersonajeItemViewModel(Personaje modelo, ServiciosDeItem servicios)
    {
        Modelo = modelo;
        _servicios = servicios;
        EsFavorito = servicios.Favoritos.EsFavorito(modelo.Id);
    }

    public Personaje Modelo { get; }

    // Datos del modelo que muestra la tarjeta. Se exponen de solo lectura
    // para que el XAML enlace contra el ViewModel y nunca directo al modelo.
    public int Id => Modelo.Id;
    public string Nombre => Modelo.Nombre;
    public string Subtitulo => Modelo.Subtitulo;
    public string ImagenUrl => Modelo.ImagenUrl;
    public string ColorEstado => Modelo.ColorEstado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconoFavorito))]
    [NotifyPropertyChangedFor(nameof(TextoAccionFavorito))]
    public partial bool EsFavorito { get; set; }

    public string IconoFavorito => EsFavorito ? "★" : "☆";

    /// <summary>Rótulo de la acción de deslizar, que cambia según el estado.</summary>
    public string TextoAccionFavorito => EsFavorito ? "Quitar" : "Favorito";

    [RelayCommand]
    private async Task AlternarFavoritoAsync()
    {
        try
        {
            EsFavorito = await _servicios.Favoritos.AlternarAsync(Modelo);
        }
        catch (Exception)
        {
            // La tarjeta no tiene dónde mostrar un mensaje: si no se pudo
            // guardar, la estrella queda como estaba y no vibra, que es la
            // señal de que el toque no tuvo efecto.
            return;
        }

        _servicios.Haptica.ConfirmarToque();
    }

    [RelayCommand]
    private Task VerDetalleAsync() => _servicios.Navegacion.IrADetalleAsync(Modelo);
}
