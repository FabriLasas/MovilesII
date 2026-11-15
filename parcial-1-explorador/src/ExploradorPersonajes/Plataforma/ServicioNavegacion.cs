using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.ViewModels;
using ExploradorPersonajes.Vistas;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Implementación de la navegación con Shell. Es la única clase que conoce
/// las rutas y el formato de los parámetros.
/// </summary>
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
