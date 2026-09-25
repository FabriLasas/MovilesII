using System.Text.Json.Serialization;

namespace ExploradorPersonajes.Modelos;

/// <summary>
/// Objeto anidado que la API usa tanto para el origen como para la
/// ubicación actual de un personaje.
/// </summary>
public class Ubicacion
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
