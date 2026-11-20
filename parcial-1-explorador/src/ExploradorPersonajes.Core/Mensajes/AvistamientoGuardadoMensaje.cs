using ExploradorPersonajes.Datos;

namespace ExploradorPersonajes.Mensajes;

/// <summary>
/// Aviso de que se guardó un avistamiento. Lo escucha el detalle del
/// personaje para sumarlo a su lista sin volver a consultar la base.
/// </summary>
public record AvistamientoGuardadoMensaje(Avistamiento Avistamiento);
