using ExploradorPersonajes.Datos;

namespace ExploradorPersonajes.Validaciones;

/// <summary>
/// Reglas de negocio de un avistamiento. Viven en el núcleo y no en la
/// pantalla: así se aplican igual se guarde desde donde se guarde, y se
/// prueban sin interfaz gráfica.
/// </summary>
public static class ValidadorAvistamiento
{
    public const int LargoMinimoNota = 3;
    public const int LargoMaximoNota = 200;

    // Nombres de campo: la pantalla los usa para mostrar cada error debajo
    // del dato que corresponde.
    public const string CampoPersonaje = nameof(Avistamiento.PersonajeId);
    public const string CampoFoto = nameof(Avistamiento.RutaFoto);
    public const string CampoNota = nameof(Avistamiento.Nota);
    public const string CampoUbicacion = "Ubicacion";

    public static ResultadoValidacion Validar(Avistamiento avistamiento)
    {
        var resultado = new ResultadoValidacion()
            .Agregar(CampoPersonaje, ReglasComunes.IdPositivo(avistamiento.PersonajeId, "el personaje avistado"))
            .Agregar(CampoFoto, string.IsNullOrWhiteSpace(avistamiento.RutaFoto)
                ? "Sacá una foto o elegila de la galería."
                : null)
            .Agregar(CampoNota, ReglasComunes.Obligatorio(avistamiento.Nota, "la nota"))
            .Agregar(CampoNota, ReglasComunes.Largo(avistamiento.Nota, LargoMinimoNota, LargoMaximoNota, "la nota"));

        ValidarUbicacion(avistamiento, resultado);

        return resultado;
    }

    /// <summary>
    /// La ubicación es opcional, pero si está tiene que estar completa (no
    /// sirve una latitud sin longitud) y dentro de los rangos posibles.
    /// </summary>
    private static void ValidarUbicacion(Avistamiento avistamiento, ResultadoValidacion resultado)
    {
        var (latitud, longitud) = (avistamiento.Latitud, avistamiento.Longitud);

        if (latitud is null && longitud is null)
        {
            return;
        }

        if (latitud is null || longitud is null)
        {
            resultado.Agregar(CampoUbicacion, "La ubicación está incompleta. Volvé a obtenerla o quitala.");
            return;
        }

        resultado
            .Agregar(CampoUbicacion, ReglasComunes.Latitud(latitud.Value))
            .Agregar(CampoUbicacion, ReglasComunes.Longitud(longitud.Value));
    }
}
