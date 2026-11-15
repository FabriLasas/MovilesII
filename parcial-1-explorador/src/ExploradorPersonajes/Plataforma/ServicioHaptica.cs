using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Implementación de la respuesta táctil con la API de MAUI.
/// </summary>
public class ServicioHaptica : IServicioHaptica
{
    private readonly IHapticFeedback _haptica;

    public ServicioHaptica(IHapticFeedback haptica)
    {
        _haptica = haptica;
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
}
