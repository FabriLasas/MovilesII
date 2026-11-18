using ExploradorPersonajes.Modelos;
using SQLite;

namespace ExploradorPersonajes.Datos;

/// <summary>
/// Fila de la tabla de favoritos.
///
/// Es una clase separada de <see cref="Personaje"/> a propósito: el modelo
/// refleja el JSON de la API (con objetos anidados como el origen) y esta
/// entidad refleja una tabla plana. Si la API cambia, no se rompe la base, y
/// si la base cambia, no se rompe la deserialización.
/// </summary>
[Table("favoritos")]
public class FavoritoEntidad
{
    /// <summary>Id del personaje en la API. Un personaje no puede repetirse.</summary>
    [PrimaryKey]
    public int Id { get; set; }

    [NotNull]
    public string Nombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string UbicacionActual { get; set; } = string.Empty;

    /// <summary>
    /// Posición en la lista. Se guarda explícitamente porque una tabla no
    /// tiene orden propio: sin esta columna, los favoritos podrían volver en
    /// cualquier orden.
    /// </summary>
    [Indexed]
    public int Orden { get; set; }

    public DateTime FechaAlta { get; set; }

    public static FavoritoEntidad DesdePersonaje(Personaje personaje, int orden, DateTime fechaAlta) => new()
    {
        Id = personaje.Id,
        Nombre = personaje.Nombre,
        Estado = personaje.Estado,
        Especie = personaje.Especie,
        Genero = personaje.Genero,
        ImagenUrl = personaje.ImagenUrl,
        Origen = personaje.Origen?.Nombre ?? string.Empty,
        UbicacionActual = personaje.UbicacionActual?.Nombre ?? string.Empty,
        Orden = orden,
        FechaAlta = fechaAlta
    };

    public Personaje APersonaje() => new()
    {
        Id = Id,
        Nombre = Nombre,
        Estado = Estado,
        Especie = Especie,
        Genero = Genero,
        ImagenUrl = ImagenUrl,
        Origen = new Ubicacion { Nombre = Origen },
        UbicacionActual = new Ubicacion { Nombre = UbicacionActual }
    };
}
