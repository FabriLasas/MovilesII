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
        => IrAsync(nameof(DetallePage), personaje);

    public Task IrANuevoAvistamientoAsync(Personaje personaje)
        => IrAsync(nameof(NuevoAvistamientoPage), personaje);

    /// <summary>".." es la ruta relativa de Shell para la pantalla anterior.</summary>
    public Task VolverAsync() => Shell.Current.GoToAsync("..");

    private static Task IrAsync(string ruta, Personaje personaje)
        => Shell.Current.GoToAsync(ruta, new Dictionary<string, object>
        {
            [DetalleViewModel.ClaveParametro] = personaje
        });
}
