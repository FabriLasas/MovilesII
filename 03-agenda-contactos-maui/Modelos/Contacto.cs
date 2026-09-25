using SQLite;

namespace AgendaContactosMaui.Modelos;

/// <summary>
/// Paso 1 y 2: entidad de la agenda. Los atributos de sqlite-net definen cómo
/// se crea la tabla "Contactos" en la base de datos.
/// </summary>
[Table("Contactos")]
public class Contacto
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(50), NotNull]
    public string Nombre { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Primera letra del nombre, para el círculo de la lista. No se guarda en la tabla.</summary>
    [Ignore]
    public string Inicial => string.IsNullOrWhiteSpace(Nombre) ? "?" : Nombre.Trim()[..1].ToUpper();
}
