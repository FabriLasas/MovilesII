using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.ViewModels;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Adaptador entre Shell y el ViewModel del detalle.
///
/// Shell entrega los parámetros de navegación llamando a IQueryAttributable,
/// una interfaz de MAUI. El ViewModel vive en el núcleo, que no depende de
/// MAUI, así que esta subclase agrega la interfaz y delega en Recibir. Se
/// eligió IQueryAttributable por sobre el atributo [QueryProperty] porque
/// hace explícito dónde y cómo se recibe el parámetro, en lugar de depender
/// de un mapeo por reflexión.
/// </summary>
public class DetalleViewModelShell : DetalleViewModel, IQueryAttributable
{
    public DetalleViewModelShell(
        IServicioFavoritos favoritos,
        IServicioHaptica haptica,
        IRepositorioAvistamientos avistamientos,
        IServicioFotos fotos,
        IServicioNavegacion navegacion,
        IMessenger mensajero)
        : base(favoritos, haptica, avistamientos, fotos, navegacion, mensajero)
    {
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) => Recibir(query);
}
