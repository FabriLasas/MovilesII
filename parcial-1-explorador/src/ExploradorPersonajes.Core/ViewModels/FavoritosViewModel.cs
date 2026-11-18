using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pestaña de favoritos.
///
/// Muestra al instante lo guardado en la base del dispositivo, así que la
/// pestaña funciona sin conexión. Después pide los datos actuales a la API en
/// UNA sola llamada para refrescarlos. Los cambios posteriores llegan por
/// mensaje, con el personaje completo, sin volver a consultar.
/// </summary>
public partial class FavoritosViewModel : ViewModelBase, IRecipient<FavoritoCambiadoMensaje>
{
    private readonly IServicioPersonajes _servicio;
    private readonly IServicioFavoritos _favoritos;
    private readonly ServiciosDeItem _serviciosDeItem;

    public ObservableCollection<PersonajeItemViewModel> Favoritos { get; } = new();

    [ObservableProperty]
    public partial bool EstaRefrescando { get; set; }

    public FavoritosViewModel(
        IServicioPersonajes servicio,
        IServicioFavoritos favoritos,
        ServiciosDeItem serviciosDeItem,
        IMessenger mensajero)
    {
        _servicio = servicio;
        _favoritos = favoritos;
        _serviciosDeItem = serviciosDeItem;
        Titulo = "Favoritos";

        mensajero.RegisterAll(this);

        // La página se crea recién la primera vez que se abre la pestaña, así
        // que cargar aquí equivale a cargar al entrar por primera vez.
        _ = CargarAsync();
    }

    public void Receive(FavoritoCambiadoMensaje mensaje)
    {
        var existente = Favoritos.FirstOrDefault(f => f.Id == mensaje.Personaje.Id);

        if (mensaje.EsFavorito && existente is null)
        {
            Favoritos.Add(new PersonajeItemViewModel(mensaje.Personaje, _serviciosDeItem));
        }
        else if (!mensaje.EsFavorito && existente is not null)
        {
            Favoritos.Remove(existente);
        }

        InformarCantidad();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefrescarAsync()
    {
        EstaRefrescando = true;
        await CargarAsync();
        EstaRefrescando = false;
    }

    private async Task CargarAsync()
    {
        await _favoritos.InicializarAsync();
        var guardados = _favoritos.ObtenerTodos();

        // 1) Lo guardado se muestra enseguida, haya red o no.
        Mostrar(guardados);
        InformarCantidad();

        if (guardados.Count == 0)
        {
            return;
        }

        // 2) Se piden los datos actuales a la API para refrescar lo guardado
        //    (un personaje pudo cambiar de ubicación o de estado).
        EstaCargando = true;
        var resultado = await _servicio.ObtenerPersonajesPorIdsAsync(guardados.Select(p => p.Id).ToList());
        EstaCargando = false;

        if (resultado.EsExitoso && !resultado.DesdeCache && resultado.Datos is not null)
        {
            await _favoritos.ActualizarDatosAsync(resultado.Datos);

            // Se vuelve a leer del servicio y no de la respuesta: mientras
            // viajaba, el usuario pudo marcar o desmarcar favoritos.
            Mostrar(_favoritos.ObtenerTodos());
            InformarCantidad();
        }
        else
        {
            // No es un error para el usuario: los favoritos se ven igual,
            // sólo que con los datos de la última vez.
            MostrarInfo("No se pudieron actualizar los datos: se muestran los guardados en el dispositivo.");
        }
    }

    private void Mostrar(IEnumerable<Personaje> personajes)
    {
        Favoritos.Clear();
        foreach (var personaje in personajes)
        {
            Favoritos.Add(new PersonajeItemViewModel(personaje, _serviciosDeItem));
        }
    }

    private void InformarCantidad()
    {
        MostrarInfo(Favoritos.Count switch
        {
            0 => "Todavía no marcaste favoritos. Tocá la estrella de un personaje.",
            1 => "1 personaje favorito.",
            var n => $"{n} personajes favoritos."
        });
    }
}
