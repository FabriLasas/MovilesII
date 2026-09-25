namespace ExploradorPersonajes.Modelos;

/// <summary>
/// Opción de filtrado por estado. Separa la etiqueta que ve el usuario (en
/// español) del valor que espera la API (en inglés), para que la traducción no
/// quede repartida entre el XAML y el ViewModel.
/// </summary>
public class OpcionFiltro
{
    public OpcionFiltro(string etiqueta, string? valorApi)
    {
        Etiqueta = etiqueta;
        ValorApi = valorApi;
    }

    public string Etiqueta { get; }

    /// <summary>Valor que se manda a la API. null significa "sin filtro".</summary>
    public string? ValorApi { get; }
}
