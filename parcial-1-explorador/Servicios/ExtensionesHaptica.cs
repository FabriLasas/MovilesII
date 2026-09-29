namespace ExploradorPersonajes.Servicios;

public static class ExtensionesHaptica
{
    /// <summary>
    /// Vibración corta de confirmación. No todos los dispositivos la soportan
    /// (una PC con Windows no tiene motor de vibración), así que se consulta
    /// antes y cualquier fallo se ignora: la respuesta táctil es un extra, no
    /// algo por lo que valga la pena interrumpir la acción del usuario.
    /// </summary>
    public static void VibrarSiEsPosible(this IHapticFeedback haptica)
    {
        if (!haptica.IsSupported) return;

        try
        {
            haptica.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }
}
