namespace ExploradorPersonajes.Validaciones;

/// <summary>Un dato que no cumple una regla, y el mensaje para el usuario.</summary>
public record ErrorValidacion(string Campo, string Mensaje);

/// <summary>
/// Resultado de validar un objeto. Junta todos los errores en lugar de
/// cortar en el primero: así la pantalla puede marcar todos los campos con
/// problemas de una sola vez, en vez de que el usuario los descubra de a uno.
/// </summary>
public class ResultadoValidacion
{
    private readonly List<ErrorValidacion> _errores = new();

    public bool EsValido => _errores.Count == 0;

    public IReadOnlyList<ErrorValidacion> Errores => _errores;

    /// <summary>Mensaje del primer error de ese campo, o null si el campo está bien.</summary>
    public string? ErrorDe(string campo) => _errores.FirstOrDefault(e => e.Campo == campo)?.Mensaje;

    /// <summary>
    /// Registra el resultado de una regla. Las reglas devuelven null cuando
    /// el dato es correcto, así que se pueden encadenar sin condicionales.
    /// </summary>
    public ResultadoValidacion Agregar(string campo, string? mensaje)
    {
        if (mensaje is not null && ErrorDe(campo) is null)
        {
            _errores.Add(new ErrorValidacion(campo, mensaje));
        }

        return this;
    }
}
