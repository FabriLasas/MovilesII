using System.Globalization;
using SQLite;

namespace ExploradorPersonajes.Datos;

/// <summary>
/// Registro de que el usuario "vio" a un personaje: una foto, una nota y,
/// si dio permiso, el lugar donde la tomó.
/// </summary>
[Table("avistamientos")]
public class Avistamiento
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Indexado porque la consulta habitual es "los de este personaje".</summary>
    [Indexed]
    public int PersonajeId { get; set; }

    /// <summary>Ruta de la foto, copiada a la carpeta de datos de la aplicación.</summary>
    public string RutaFoto { get; set; } = string.Empty;

    public string Nota { get; set; } = string.Empty;

    /// <summary>
    /// Opcionales: si el usuario no da permiso de ubicación, el avistamiento
    /// se guarda igual, sin coordenadas.
    /// </summary>
    public double? Latitud { get; set; }

    public double? Longitud { get; set; }

    public DateTime Fecha { get; set; }

    // --- Propiedades calculadas para la vista (no son columnas) ---

    [Ignore]
    public bool TieneUbicacion => Latitud is not null && Longitud is not null;

    [Ignore]
    public string TextoUbicacion => TieneUbicacion
        ? string.Create(CultureInfo.InvariantCulture, $"{Latitud:F5}, {Longitud:F5}")
        : "Sin ubicación";

    [Ignore]
    public string TextoFecha => Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}
