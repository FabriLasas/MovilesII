using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;
using ExploradorPersonajes.ViewModels;
using Moq;

namespace ExploradorPersonajes.Tests.Dobles;

/// <summary>
/// Agrupa los mocks que necesitan los ViewModels. Cada prueba crea uno nuevo,
/// configura sólo lo que le importa y después verifica las llamadas.
/// </summary>
public class ServiciosSimulados
{
    public Mock<IServicioPersonajes> Api { get; } = new();
    public Mock<IServicioFavoritos> Favoritos { get; } = new();
    public Mock<IServicioNavegacion> Navegacion { get; } = new();
    public Mock<IServicioHaptica> Haptica { get; } = new();
    public Mock<IServicioConectividad> Conectividad { get; } = new();

    /// <summary>
    /// Mensajero real pero propio de la prueba: si se usara el global
    /// (WeakReferenceMessenger.Default), las pruebas se enviarían mensajes
    /// entre sí.
    /// </summary>
    public IMessenger Mensajero { get; } = new WeakReferenceMessenger();

    public ServiciosSimulados()
    {
        Favoritos.Setup(f => f.ObtenerIds()).Returns(Array.Empty<int>());
        Navegacion.Setup(n => n.IrADetalleAsync(It.IsAny<Personaje>())).Returns(Task.CompletedTask);
    }

    public ServiciosDeItem DeItem() => new(Favoritos.Object, Navegacion.Object, Haptica.Object);

    public PersonajesViewModel CrearPersonajesViewModel()
        => new(Api.Object, DeItem(), Conectividad.Object, Mensajero);

    public FavoritosViewModel CrearFavoritosViewModel()
        => new(Api.Object, Favoritos.Object, DeItem(), Mensajero);

    public DetalleViewModel CrearDetalleViewModel()
        => new(Favoritos.Object, Haptica.Object);

    /// <summary>Configura la respuesta de la API para una página dada.</summary>
    public void ResponderPagina(int pagina, ResultadoApi<RespuestaPaginada> resultado)
        => Api.Setup(a => a.ObtenerPersonajesAsync(pagina, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(resultado);
}
