namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Respuesta táctil del dispositivo. Reemplaza el uso directo de
/// IHapticFeedback de MAUI en los ViewModels: con la interfaz propia el
/// núcleo no depende de la plataforma y las pruebas pueden verificar que la
/// vibración se pidió sin tener un teléfono.
/// </summary>
public interface IServicioHaptica
{
    /// <summary>
    /// Vibración corta de confirmación. Si el dispositivo no la soporta, no
    /// hace nada: nunca interrumpe la acción del usuario.
    /// </summary>
    void ConfirmarToque();
}
