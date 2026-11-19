namespace ExploradorPersonajes.Validaciones;

/// <summary>
/// Reglas de validación reutilizables. Cada una devuelve el mensaje de error
/// o null si el dato es correcto.
///
/// Se centralizan acá para que la misma regla (por ejemplo, el largo máximo
/// de un texto) no se escriba de nuevo en cada pantalla con mensajes
/// distintos, y para poder probarlas una sola vez. Las usan el validador de
/// avistamientos y la búsqueda de personajes.
/// </summary>
public static class ReglasComunes
{
    /// <param name="etiqueta">Nombre del dato con su artículo, por ejemplo "la nota".</param>
    public static string? Obligatorio(string? valor, string etiqueta)
        => string.IsNullOrWhiteSpace(valor) ? $"Completá {etiqueta}." : null;

    /// <summary>
    /// Largo entre un mínimo y un máximo. Se cuenta sin los espacios de los
    /// extremos: "   a   " no debería pasar como un texto de 7 caracteres.
    /// </summary>
    public static string? Largo(string? valor, int minimo, int maximo, string etiqueta)
    {
        var largo = (valor ?? string.Empty).Trim().Length;

        return largo < minimo || largo > maximo
            ? $"{Capitalizar(etiqueta)} debe tener entre {minimo} y {maximo} caracteres."
            : null;
    }

    public static string? LargoMaximo(string? valor, int maximo, string etiqueta)
        => (valor ?? string.Empty).Trim().Length > maximo
            ? $"{Capitalizar(etiqueta)} no puede superar los {maximo} caracteres."
            : null;

    public static string? IdPositivo(int id, string etiqueta)
        => id > 0 ? null : $"Falta {etiqueta}.";

    public static string? Latitud(double latitud)
        => latitud is >= -90 and <= 90 && !double.IsNaN(latitud)
            ? null
            : "La latitud debe estar entre -90 y 90 grados.";

    public static string? Longitud(double longitud)
        => longitud is >= -180 and <= 180 && !double.IsNaN(longitud)
            ? null
            : "La longitud debe estar entre -180 y 180 grados.";

    private static string Capitalizar(string texto)
        => string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto[1..];
}
