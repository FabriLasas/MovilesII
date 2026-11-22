namespace ExploradorPersonajes.Comportamientos;

/// <summary>
/// Hace que un elemento aparezca con un fundido y un leve desplazamiento
/// hacia arriba en lugar de surgir de golpe.
///
/// Se implementa como Behavior porque una animación necesita acceso directo
/// al control visual, algo que el ViewModel no debe tener. Así la animación
/// se declara en el XAML, no requiere code-behind y se reutiliza en cualquier
/// elemento.
/// </summary>
public class AparicionGradualBehavior : Behavior<VisualElement>
{
    private const uint Duracion = 260;
    private const double DesplazamientoInicial = 14;

    protected override void OnAttachedTo(VisualElement elemento)
    {
        base.OnAttachedTo(elemento);
        elemento.Opacity = 0;
        elemento.TranslationY = DesplazamientoInicial;
        elemento.Loaded += AlCargarse;
    }

    protected override void OnDetachingFrom(VisualElement elemento)
    {
        elemento.Loaded -= AlCargarse;
        base.OnDetachingFrom(elemento);
    }

    private static async void AlCargarse(object? sender, EventArgs e)
    {
        if (sender is not VisualElement elemento) return;

        // Ambas animaciones corren en paralelo. CubicOut arranca rápido y
        // frena al final, que es como se percibe natural una entrada.
        await Task.WhenAll(
            elemento.FadeToAsync(1, Duracion, Easing.CubicOut),
            elemento.TranslateToAsync(0, 0, Duracion, Easing.CubicOut));
    }
}
