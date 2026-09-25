namespace AgendaContactosMaui.ViewModels;

/// <summary>
/// Vibración corta como respuesta a una acción. En plataformas sin motor de
/// vibración (Windows) no hace nada en vez de lanzar una excepción.
/// </summary>
internal static class Haptica
{
    public static void Vibrar(HapticFeedbackType tipo = HapticFeedbackType.Click)
    {
        if (HapticFeedback.Default.IsSupported)
            HapticFeedback.Default.Perform(tipo);
    }
}
