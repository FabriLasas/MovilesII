namespace ExploradorPersonajes.Datos;

/// <summary>Acceso a los avistamientos guardados en el dispositivo.</summary>
public interface IRepositorioAvistamientos
{
    /// <summary>Guarda un avistamiento nuevo y le asigna su Id.</summary>
    Task GuardarAsync(Avistamiento avistamiento);

    /// <summary>Avistamientos de un personaje, del más reciente al más antiguo.</summary>
    Task<List<Avistamiento>> ObtenerPorPersonajeAsync(int personajeId);

    Task EliminarAsync(int id);
}
