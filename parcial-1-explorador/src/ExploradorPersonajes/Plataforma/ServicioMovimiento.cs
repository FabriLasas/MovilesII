using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Detección de agitado con el acelerómetro de MAUI. MAUI ya trae el cálculo
/// (el evento ShakeDetected compara las aceleraciones sucesivas contra un
/// umbral), así que no hace falta procesar cada lectura a mano.
/// </summary>
public class ServicioMovimiento : IServicioMovimiento
{
    private readonly IAccelerometer _acelerometro;

    public ServicioMovimiento(IAccelerometer acelerometro)
    {
        _acelerometro = acelerometro;
        _acelerometro.ShakeDetected += AlDetectarAgitado;
    }

    public bool EstaDisponible => _acelerometro.IsSupported;

    public event EventHandler? Agitado;

    /// <summary>
    /// SensorSpeed.Game es la velocidad que recomienda la documentación para
    /// detectar agitados: con UI las lecturas son tan espaciadas que un
    /// movimiento corto puede pasar desapercibido.
    /// </summary>
    public void Iniciar()
    {
        if (!EstaDisponible || _acelerometro.IsMonitoring) return;

        try
        {
            _acelerometro.Start(SensorSpeed.Game);
        }
        catch (FeatureNotSupportedException)
        {
        }
    }

    public void Detener()
    {
        if (!_acelerometro.IsMonitoring) return;

        try
        {
            _acelerometro.Stop();
        }
        catch (FeatureNotSupportedException)
        {
        }
    }

    /// <summary>
    /// El sensor avisa desde otro hilo; la pantalla sólo se puede tocar desde
    /// el principal.
    /// </summary>
    private void AlDetectarAgitado(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(() => Agitado?.Invoke(this, EventArgs.Empty));
}
