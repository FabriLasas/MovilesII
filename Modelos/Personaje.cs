using System.Text.Json.Serialization;

namespace ExploradorPersonajes.Modelos;

/// <summary>
/// Representa un personaje devuelto por la API de Rick and Morty.
/// Los atributos JsonPropertyName mapean los nombres del JSON (en inglés
/// y con snake_case) a nombres de propiedades significativos en español.
/// </summary>
public class Personaje
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("species")]
    public string Especie { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Genero { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string ImagenUrl { get; set; } = string.Empty;

    /// <summary>Objeto anidado: el JSON trae origin como { name, url }.</summary>
    [JsonPropertyName("origin")]
    public Ubicacion? Origen { get; set; }

    /// <summary>Objeto anidado: ubicación actual del personaje.</summary>
    [JsonPropertyName("location")]
    public Ubicacion? UbicacionActual { get; set; }

    // --- Propiedades calculadas para la vista ---
    // No vienen de la API: se derivan de los datos para no ensuciar el XAML
    // con convertidores ni poner lógica de presentación en el code-behind.

    [JsonIgnore]
    public string EstadoTraducido => Estado switch
    {
        "Alive" => "Vivo",
        "Dead" => "Muerto",
        _ => "Desconocido"
    };

    [JsonIgnore]
    public string ColorEstado => Estado switch
    {
        "Alive" => "#2E9E5B",
        "Dead" => "#C0392B",
        _ => "#7F8C8D"
    };

    [JsonIgnore]
    public string Subtitulo => $"{EstadoTraducido} · {Especie}";
}
