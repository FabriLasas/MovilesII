using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.ViewModels;
using ExploradorPersonajes.Vistas;

namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Encapsula la navegación. Los ViewModels piden "ir al detalle de este
/// personaje" sin conocer Shell, rutas ni diccionarios de parámetros. Así la
/// navegación se resuelve en un solo lugar aunque la pidan varias pantallas.
/// </summary>
public interface IServicioNavegacion
{
    Task IrADetalleAsync(Personaje personaje);
}

public class ServicioNavegacion : IServicioNavegacion
{
    public Task IrADetalleAsync(Personaje personaje)
    {
        return Shell.Current.GoToAsync(nameof(DetallePage), new Dictionary<string, object>
        {
            [DetalleViewModel.ClaveParametro] = personaje
        });
    }
}
