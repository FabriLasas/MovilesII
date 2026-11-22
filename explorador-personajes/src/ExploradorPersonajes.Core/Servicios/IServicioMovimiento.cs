namespace ExploradorPersonajes.Servicios;

/// <summary>
/// Detección de que el usuario agitó el teléfono, a partir del acelerómetro.
///
/// El sensor consume batería mientras está encendido, así que no se deja
/// prendido todo el tiempo: la pantalla que lo usa lo inicia al aparecer y
/// lo detiene al dejar de verse.
/// </summary>
public interface IServicioMovimiento
{
    /// <summary>El dispositivo tiene acelerómetro (una PC, por ejemplo, no).</summary>
    bool EstaDisponible { get; }

    /// <summary>Se dispara en el hilo principal cada vez que se detecta un agitado.</summary>
    event EventHandler? Agitado;

    void Iniciar();

    void Detener();
}
