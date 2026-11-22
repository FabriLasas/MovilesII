namespace ExploradorPersonajes.Servicios;

/// <summary>Respuesta a un pedido de permiso, simplificada para la lógica de la app.</summary>
public enum EstadoPermiso
{
    Concedido,

    /// <summary>El usuario lo negó. Puede volver a habilitarlo desde Ajustes.</summary>
    Denegado,

    /// <summary>El dispositivo no tiene ese hardware o el sistema no permite pedirlo.</summary>
    NoDisponible
}

/// <summary>
/// Comprobación y pedido de permisos en tiempo de ejecución. Cada pedido
/// primero consulta el estado actual y solo muestra el diálogo del sistema si
/// el permiso todavía no fue concedido.
/// </summary>
public interface IServicioPermisos
{
    Task<EstadoPermiso> SolicitarCamaraAsync();

    /// <summary>Ubicación solo mientras la app está en uso, nunca en segundo plano.</summary>
    Task<EstadoPermiso> SolicitarUbicacionAsync();

    /// <summary>
    /// Abre la pantalla de ajustes de la aplicación. Es la única forma de
    /// recuperar un permiso que el usuario negó con "no volver a preguntar".
    /// </summary>
    void AbrirAjustes();
}
