using System.Text;
using AgendaContactos.Data;
using AgendaContactos.Models;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var rutaDb = Path.Combine(AppContext.BaseDirectory, "agenda.db3");
var agenda = new AgendaDatabase(rutaDb);

// Paso 3a: si la agenda está vacía, cargamos 3 contactos de ejemplo.
if (agenda.Cantidad() == 0)
{
    agenda.Insertar(new Contacto { Nombre = "Ana García", Telefono = "11-4567-8901", Email = "ana.garcia@mail.com" });
    agenda.Insertar(new Contacto { Nombre = "Bruno Pérez", Telefono = "11-2345-6789", Email = "bruno.perez@mail.com" });
    agenda.Insertar(new Contacto { Nombre = "Carla López", Telefono = "11-9876-5432", Email = "carla.lopez@mail.com" });
    Console.WriteLine("Se cargaron 3 contactos de ejemplo.\n");
}

Console.WriteLine($"Base de datos: {rutaDb}");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("===== AGENDA DE CONTACTOS =====");
    Console.WriteLine("1. Listar contactos");
    Console.WriteLine("2. Agregar contacto");
    Console.WriteLine("3. Buscar por nombre");
    Console.WriteLine("4. Actualizar teléfono / email");
    Console.WriteLine("5. Eliminar contacto por Id");
    Console.WriteLine("0. Salir");
    var opcion = Leer("Opción: ");

    try
    {
        switch (opcion)
        {
            case "1": Listar(); break;
            case "2": Agregar(); break;
            case "3": Buscar(); break;
            case "4": Actualizar(); break;
            case "5": Eliminar(); break;
            case "0": return;
            default: Console.WriteLine("Opción inválida."); break;
        }
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// Paso 3b
void Listar()
{
    var contactos = agenda.ObtenerTodos();
    if (contactos.Count == 0)
    {
        Console.WriteLine("La agenda está vacía.");
        return;
    }
    Console.WriteLine($"\n{"Id",-4} {"Nombre",-25} {"Teléfono",-15} Email");
    Console.WriteLine(new string('-', 75));
    foreach (var c in contactos)
        Console.WriteLine($"{c.Id,-4} {c.Nombre,-25} {c.Telefono,-15} {c.Email}");
}

void Agregar()
{
    var c = new Contacto
    {
        Nombre = Leer($"Nombre (máx. {AgendaDatabase.MaxNombre}): "),
        Telefono = Leer("Teléfono: "),
        Email = Leer("Email: ")
    };
    agenda.Insertar(c);
    Console.WriteLine($"Contacto agregado con Id {c.Id}.");
}

// Paso 3c
void Buscar()
{
    var texto = Leer("Nombre a buscar: ");
    var encontrados = agenda.BuscarPorNombre(texto);
    if (encontrados.Count == 0)
    {
        Console.WriteLine("No se encontraron contactos.");
        return;
    }
    foreach (var c in encontrados)
        MostrarDetalle(c);
}

// Extra: actualizar teléfono o email
void Actualizar()
{
    var c = PedirContactoPorId();
    if (c is null) return;

    MostrarDetalle(c);
    Console.WriteLine("(Enter para dejar el valor actual)");
    var tel = Leer("Nuevo teléfono: ");
    var mail = Leer("Nuevo email: ");
    if (tel != "") c.Telefono = tel;
    if (mail != "") c.Email = mail;

    agenda.Actualizar(c);
    Console.WriteLine("Contacto actualizado:");
    MostrarDetalle(c);
}

// Extra: eliminar por Id
void Eliminar()
{
    var c = PedirContactoPorId();
    if (c is null) return;

    var confirma = Leer($"¿Eliminar a {c.Nombre}? (s/n): ");
    if (confirma.Equals("s", StringComparison.OrdinalIgnoreCase))
    {
        agenda.Eliminar(c.Id);
        Console.WriteLine("Contacto eliminado.");
    }
    else
    {
        Console.WriteLine("Operación cancelada.");
    }
}

Contacto? PedirContactoPorId()
{
    if (!int.TryParse(Leer("Id del contacto: "), out var id))
    {
        Console.WriteLine("Id inválido.");
        return null;
    }
    var c = agenda.ObtenerPorId(id);
    if (c is null)
        Console.WriteLine($"No existe un contacto con Id {id}.");
    return c;
}

void MostrarDetalle(Contacto c)
{
    Console.WriteLine();
    Console.WriteLine($"  Id:       {c.Id}");
    Console.WriteLine($"  Nombre:   {c.Nombre}");
    Console.WriteLine($"  Teléfono: {c.Telefono}");
    Console.WriteLine($"  Email:    {c.Email}");
}

string Leer(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine()?.Trim() ?? "";
}
