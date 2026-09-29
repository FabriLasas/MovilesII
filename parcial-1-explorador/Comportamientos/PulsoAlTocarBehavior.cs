namespace ExploradorPersonajes.Comportamientos;

/// <summary>
/// Agranda brevemente un botón al tocarlo y lo devuelve a su tamaño con un
/// pequeño rebote. Confirma visualmente la acción, del mismo modo que la
/// vibración la confirma al tacto.
///
/// Escucha el evento Clicked, que el botón dispara además de ejecutar su
/// Command: la acción sigue en el ViewModel y la animación queda en la vista.
/// </summary>
public class PulsoAlTocarBehavior : Behavior<Button>
{
    protected override void OnAttachedTo(Button boton)
    {
        base.OnAttachedTo(boton);
        boton.Clicked += AlTocar;
    }

    protected override void OnDetachingFrom(Button boton)
    {
        boton.Clicked -= AlTocar;
        base.OnDetachingFrom(boton);
    }

    private static async void AlTocar(object? sender, EventArgs e)
    {
        if (sender is not Button boton) return;

        // Si el usuario toca varias veces seguidas, se cancela el pulso
        // anterior para no acumular escalas a medio terminar.
        boton.CancelAnimations();

        await boton.ScaleToAsync(1.35, 90, Easing.CubicOut);
        await boton.ScaleToAsync(1.0, 220, Easing.SpringOut);
    }
}
