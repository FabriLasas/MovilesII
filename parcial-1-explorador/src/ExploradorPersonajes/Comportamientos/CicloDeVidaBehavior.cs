using System.Windows.Input;

namespace ExploradorPersonajes.Comportamientos;

/// <summary>
/// Ejecuta un comando del ViewModel cuando la página aparece y otro cuando
/// deja de verse. Permite que el ViewModel encienda y apague recursos (como
/// un sensor) según la pantalla esté visible, sin escribir lógica en el
/// code-behind.
/// </summary>
public class CicloDeVidaBehavior : Behavior<Page>
{
    public static readonly BindableProperty AlAparecerProperty =
        BindableProperty.Create(nameof(AlAparecer), typeof(ICommand), typeof(CicloDeVidaBehavior));

    public static readonly BindableProperty AlDesaparecerProperty =
        BindableProperty.Create(nameof(AlDesaparecer), typeof(ICommand), typeof(CicloDeVidaBehavior));

    private Page? _pagina;

    public ICommand? AlAparecer
    {
        get => (ICommand?)GetValue(AlAparecerProperty);
        set => SetValue(AlAparecerProperty, value);
    }

    public ICommand? AlDesaparecer
    {
        get => (ICommand?)GetValue(AlDesaparecerProperty);
        set => SetValue(AlDesaparecerProperty, value);
    }

    protected override void OnAttachedTo(Page pagina)
    {
        base.OnAttachedTo(pagina);
        _pagina = pagina;

        // Un Behavior no hereda el BindingContext de la página: se copia a
        // mano para que los {Binding} de sus propiedades encuentren al ViewModel.
        BindingContext = pagina.BindingContext;
        pagina.BindingContextChanged += AlCambiarContexto;
        pagina.Appearing += AlAparecerPagina;
        pagina.Disappearing += AlDesaparecerPagina;
    }

    protected override void OnDetachingFrom(Page pagina)
    {
        pagina.BindingContextChanged -= AlCambiarContexto;
        pagina.Appearing -= AlAparecerPagina;
        pagina.Disappearing -= AlDesaparecerPagina;
        _pagina = null;
        base.OnDetachingFrom(pagina);
    }

    private void AlCambiarContexto(object? sender, EventArgs e) => BindingContext = _pagina?.BindingContext;

    private void AlAparecerPagina(object? sender, EventArgs e) => Ejecutar(AlAparecer);

    private void AlDesaparecerPagina(object? sender, EventArgs e) => Ejecutar(AlDesaparecer);

    private static void Ejecutar(ICommand? comando)
    {
        if (comando?.CanExecute(null) == true)
        {
            comando.Execute(null);
        }
    }
}
