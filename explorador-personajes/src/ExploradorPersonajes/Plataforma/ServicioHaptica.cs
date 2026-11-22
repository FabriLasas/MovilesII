using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Respuesta táctil con las APIs de MAUI: HapticFeedback para el toque corto
/// y Vibration para la vibración de duración elegida.
/// </summary>
public class ServicioHaptica : IServicioHaptica
{
    private readonly IHapticFeedback _haptica;
    private readonly IVibration _vibracion;

    public ServicioHaptica(IHapticFeedback haptica, IVibration vibracion)
    {
        _haptica = haptica;
        _vibracion = vibracion;
    }

    /// <summary>
    /// No todos los dispositivos la soportan (una PC con Windows no tiene
    /// motor de vibración), así que se consulta antes y cualquier fallo se
    /// ignora: la respuesta táctil es un extra, no algo por lo que valga la
    /// pena interrumpir la acción del usuario.
    /// </summary>
    public void ConfirmarToque()
    {
        if (!_haptica.IsSupported) return;

        try
        {
            _haptica.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }

    public void Vibrar(TimeSpan duracion)
    {
        if (!_vibracion.IsSupported) return;

        try
        {
            _vibracion.Vibrate(duracion);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }
}
