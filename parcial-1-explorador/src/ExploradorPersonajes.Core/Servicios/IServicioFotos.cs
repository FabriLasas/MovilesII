namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Obtención de fotos con la cámara o la galería.
///
/// Las fotos pasan por dos carpetas: primero una temporal, mientras el
/// usuario completa el formulario, y recién al guardar el avistamiento se
/// copian a la carpeta definitiva de la app. Lo que queda en la temporal
/// (un formulario abandonado) lo limpia la app al iniciar y, si falta
/// espacio, el propio sistema.
/// </summary>
public interface IServicioFotos
{
    /// <summary>El dispositivo tiene cámara y una app capaz de sacar fotos.</summary>
    bool CamaraDisponible { get; }

    /// <summary>Abre la cámara. Devuelve la ruta temporal de la foto, o null si el usuario canceló.</summary>
    Task<string?> TomarAsync();

    /// <summary>Abre la galería. Devuelve la ruta temporal de la copia, o null si el usuario canceló.</summary>
    Task<string?> ElegirDeGaleriaAsync();

    /// <summary>Copia una foto temporal a la carpeta definitiva y devuelve la ruta nueva.</summary>
    Task<string> ConservarAsync(string rutaTemporal);

    /// <summary>Borra una foto de la app, temporal o definitiva. Nunca toca archivos de otras carpetas.</summary>
    void Eliminar(string ruta);
}
