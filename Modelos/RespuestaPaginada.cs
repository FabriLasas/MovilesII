using System.Text.Json.Serialization;

namespace ExploradorPersonajes.Modelos;

/// <summary>
/// La API no devuelve una lista plana: envuelve los resultados en un objeto
/// con metadatos de paginación. Por eso hace falta un modelo contenedor
/// además del modelo de cada elemento.
/// </summary>
public class RespuestaPaginada
{
    [JsonPropertyName("info")]
    public InfoPaginacion? Info { get; set; }

    [JsonPropertyName("results")]
    public List<Personaje> Resultados { get; set; } = new();
}

public class InfoPaginacion
{
    [JsonPropertyName("count")]
    public int TotalElementos { get; set; }

    [JsonPropertyName("pages")]
    public int TotalPaginas { get; set; }

    /// <summary>URL de la página siguiente, o null si es la última.</summary>
    [JsonPropertyName("next")]
    public string? PaginaSiguiente { get; set; }
}
