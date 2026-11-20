using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pantalla de detalle: datos del personaje, favorito y sus
/// avistamientos.
///
/// Recibe el personaje con <see cref="Recibir"/>, que toma los parámetros de
/// navegación como un diccionario común. No implementa IQueryAttributable
/// porque esa interfaz es de MAUI y el núcleo no depende de la plataforma: la
/// aplicación la agrega en una subclase que sólo delega en este método.
/// </summary>
public partial class DetalleViewModel : ViewModelBase, IRecipient<AvistamientoGuardadoMensaje>
{
    /// <summary>
    /// Clave del parámetro de navegación. Constante compartida para que el
    /// emisor y el receptor no puedan desincronizarse por un error de tipeo.
    /// </summary>
    public const string ClaveParametro = "personaje";

    private readonly IServicioFavoritos _favoritos;
    private readonly IServicioHaptica _haptica;
    private readonly IRepositorioAvistamientos _avistamientos;
    private readonly IServicioFotos _fotos;
    private readonly IServicioNavegacion _navegacion;

    public DetalleViewModel(
        IServicioFavoritos favoritos,
        IServicioHaptica haptica,
        IRepositorioAvistamientos avistamientos,
        IServicioFotos fotos,
        IServicioNavegacion navegacion,
        IMessenger mensajero)
    {
        _favoritos = favoritos;
        _haptica = haptica;
        _avistamientos = avistamientos;
        _fotos = fotos;
        _navegacion = navegacion;
        ResumenAvistamientos = string.Empty;

        mensajero.RegisterAll(this);
    }

    [ObservableProperty]
    public partial Personaje? Personaje { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoBotonFavorito))]
    public partial bool EsFavorito { get; set; }

    public string TextoBotonFavorito => EsFavorito ? "★  Quitar de favoritos" : "☆  Agregar a favoritos";

    public ObservableCollection<AvistamientoItemViewModel> Avistamientos { get; } = new();

    [ObservableProperty]
    public partial string ResumenAvistamientos { get; set; }

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

            _ = CargarAvistamientosAsync();
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
    private async Task AlternarFavoritoAsync()
    {
        if (Personaje is null) return;

        EsFavorito = await _favoritos.AlternarAsync(Personaje);
        _haptica.ConfirmarToque();
    }

    // --- Avistamientos ---

    public async Task CargarAvistamientosAsync()
    {
        if (Personaje is null) return;

        try
        {
            var lista = await _avistamientos.ObtenerPorPersonajeAsync(Personaje.Id);

            Avistamientos.Clear();
            foreach (var avistamiento in lista)
            {
                Avistamientos.Add(CrearItem(avistamiento));
            }
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudieron leer los avistamientos. ({ex.Message})");
        }

        ActualizarResumen();
    }

    [RelayCommand]
    private Task NuevoAvistamientoAsync()
        => Personaje is null ? Task.CompletedTask : _navegacion.IrANuevoAvistamientoAsync(Personaje);

    /// <summary>
    /// El avistamiento recién guardado llega por mensaje con todos sus datos:
    /// se agrega arriba de la lista sin volver a consultar la base.
    /// </summary>
    public void Receive(AvistamientoGuardadoMensaje mensaje)
    {
        if (mensaje.Avistamiento.PersonajeId != Personaje?.Id) return;

        Avistamientos.Insert(0, CrearItem(mensaje.Avistamiento));
        ActualizarResumen();
    }

    private async Task EliminarAsync(AvistamientoItemViewModel item)
    {
        try
        {
            await _avistamientos.EliminarAsync(item.Modelo.Id);
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo eliminar el avistamiento. ({ex.Message})");
            return;
        }

        // La foto sólo se borra después de quitar el registro: si fallara al
        // revés, quedaría un avistamiento apuntando a una foto inexistente.
        _fotos.Eliminar(item.Modelo.RutaFoto);
        Avistamientos.Remove(item);
        _haptica.ConfirmarToque();
        ActualizarResumen();
    }

    private AvistamientoItemViewModel CrearItem(Avistamiento avistamiento) => new(avistamiento, EliminarAsync);

    private void ActualizarResumen()
    {
        ResumenAvistamientos = Avistamientos.Count switch
        {
            0 => "Todavía no lo avistaste. Registrá el primero con una foto.",
            1 => "1 avistamiento",
            var n => $"{n} avistamientos"
        };
    }
}
