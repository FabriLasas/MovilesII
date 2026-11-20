using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.ViewModels;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Adaptador entre Shell y el formulario de avistamiento, con el mismo
/// criterio que <see cref="DetalleViewModelShell"/>.
/// </summary>
public class NuevoAvistamientoViewModelShell : NuevoAvistamientoViewModel, IQueryAttributable
{
    public NuevoAvistamientoViewModelShell(
        IServicioPermisos permisos,
        IServicioFotos fotos,
        IServicioUbicacion ubicacion,
        IRepositorioAvistamientos repositorio,
        IServicioHaptica haptica,
        IServicioNavegacion navegacion,
        IMessenger mensajero)
        : base(permisos, fotos, ubicacion, repositorio, haptica, navegacion, mensajero)
    {
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) => Recibir(query);
}
