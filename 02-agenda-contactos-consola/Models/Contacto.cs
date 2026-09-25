using SQLite;

namespace AgendaContactos.Models;

// Paso 1 y 2: la clase se mapea a la tabla "Contactos" mediante atributos de sqlite-net.
[Table("Contactos")]
public class Contacto
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(50), NotNull]
    public string Nombre { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public override string ToString() =>
        $"[{Id}] {Nombre} | Tel: {Telefono} | Email: {Email}";
}
