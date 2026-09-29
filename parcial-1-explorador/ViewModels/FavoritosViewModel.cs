using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Mensajes;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.ViewModels;

/// <summary>
/// ViewModel de la pestaña de favoritos.
///
/// La primera carga trae a todos los favoritos en UNA sola llamada a la API.
/// Después se mantiene al día escuchando los cambios, sin volver a consultar:
/// el mensaje trae el personaje completo, así que agregarlo o quitarlo de la
/// lista funciona incluso sin conexión.
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
        var ids = _favoritos.ObtenerIds();

        if (ids.Count == 0)
        {
            Favoritos.Clear();
            OcultarAvisoCache();
            InformarCantidad();
            return;
        }

        EstaCargando = true;
        MostrarInfo("Cargando favoritos...");

        var resultado = await _servicio.ObtenerPersonajesPorIdsAsync(ids);

        if (resultado.EsExitoso && resultado.Datos is not null)
        {
            // Mientras viajaba la respuesta el usuario pudo marcar o desmarcar
            // favoritos (esos cambios llegan por mensaje). Se reconcilia contra
            // la lista vigente para no perder ni resucitar ninguno.
            var vigentes = _favoritos.ObtenerIds();
            var recibidos = resultado.Datos.Select(p => p.Id).ToHashSet();
            var agregadosMientras = Favoritos
                .Where(f => !recibidos.Contains(f.Id) && vigentes.Contains(f.Id))
                .Select(f => f.Modelo)
                .ToList();

            Favoritos.Clear();
            foreach (var personaje in resultado.Datos.Where(p => vigentes.Contains(p.Id)).Concat(agregadosMientras))
            {
                Favoritos.Add(new PersonajeItemViewModel(personaje, _serviciosDeItem));
            }

            if (resultado.DesdeCache && resultado.FechaCache is { } fecha)
            {
                MostrarAvisoCache(fecha);
            }
            else
            {
                OcultarAvisoCache();
            }

            InformarCantidad();
        }
        else
        {
            MostrarError(resultado.Mensaje);
        }

        EstaCargando = false;
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
