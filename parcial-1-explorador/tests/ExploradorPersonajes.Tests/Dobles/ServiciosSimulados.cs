using CommunityToolkit.Mvvm.Messaging;
using ExploradorPersonajes.Datos;
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
    public static readonly DateTime Ahora = new(2026, 11, 20, 18, 45, 0);

    public Mock<IServicioPersonajes> Api { get; } = new();
    public Mock<IServicioFavoritos> Favoritos { get; } = new();
    public Mock<IServicioNavegacion> Navegacion { get; } = new();
    public Mock<IServicioHaptica> Haptica { get; } = new();
    public Mock<IServicioConectividad> Conectividad { get; } = new();

    // Hardware y permisos (Unidad 5)
    public Mock<IServicioPermisos> Permisos { get; } = new();
    public Mock<IServicioFotos> Fotos { get; } = new();
    public Mock<IServicioUbicacion> Ubicacion { get; } = new();
    public Mock<IRepositorioAvistamientos> Avistamientos { get; } = new();

    /// <summary>
    /// Mensajero real pero propio de la prueba: si se usara el global
    /// (WeakReferenceMessenger.Default), las pruebas se enviarían mensajes
    /// entre sí.
    /// </summary>
    public IMessenger Mensajero { get; } = new WeakReferenceMessenger();

    public ServiciosSimulados()
    {
        Favoritos.Setup(f => f.ObtenerTodos()).Returns(Array.Empty<Personaje>());
        Navegacion.Setup(n => n.IrADetalleAsync(It.IsAny<Personaje>())).Returns(Task.CompletedTask);
        Avistamientos.Setup(a => a.ObtenerPorPersonajeAsync(It.IsAny<int>())).ReturnsAsync(new List<Avistamiento>());
        Fotos.SetupGet(f => f.CamaraDisponible).Returns(true);
    }

    public ServiciosDeItem DeItem() => new(Favoritos.Object, Navegacion.Object, Haptica.Object);

    public PersonajesViewModel CrearPersonajesViewModel()
        => new(Api.Object, DeItem(), Conectividad.Object, Mensajero);

    public FavoritosViewModel CrearFavoritosViewModel()
        => new(Api.Object, Favoritos.Object, DeItem(), Mensajero);

    public DetalleViewModel CrearDetalleViewModel()
        => new(Favoritos.Object, Haptica.Object, Avistamientos.Object, Fotos.Object, Navegacion.Object, Mensajero);

    public NuevoAvistamientoViewModel CrearNuevoAvistamientoViewModel()
        => new(Permisos.Object, Fotos.Object, Ubicacion.Object, Avistamientos.Object,
               Haptica.Object, Navegacion.Object, Mensajero, () => Ahora);

    /// <summary>Configura la respuesta de la API para una página dada.</summary>
    public void ResponderPagina(int pagina, ResultadoApi<RespuestaPaginada> resultado)
        => Api.Setup(a => a.ObtenerPersonajesAsync(pagina, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(resultado);
}
