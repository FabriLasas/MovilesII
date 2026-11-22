using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Permisos en tiempo de ejecución con la API Permissions de MAUI. En
/// Android se piden con el diálogo del sistema (desde Android 6); en iOS el
/// texto que ve el usuario sale de Info.plist.
/// </summary>
public class ServicioPermisos : IServicioPermisos
{
    private readonly IAppInfo _appInfo;

    public ServicioPermisos(IAppInfo appInfo)
    {
        _appInfo = appInfo;
    }

    public Task<EstadoPermiso> SolicitarCamaraAsync() => SolicitarAsync<Permissions.Camera>();

    public Task<EstadoPermiso> SolicitarUbicacionAsync() => SolicitarAsync<Permissions.LocationWhenInUse>();

    public void AbrirAjustes() => _appInfo.ShowSettingsUI();

    /// <summary>
    /// Primero consulta el estado y solo muestra el diálogo si hace falta: si
    /// el permiso ya estaba concedido, el usuario no ve nada. Los pedidos se
    /// hacen en el hilo principal, como exige el sistema para mostrar el
    /// diálogo.
    /// </summary>
    private static Task<EstadoPermiso> SolicitarAsync<TPermiso>()
        where TPermiso : Permissions.BasePermission, new()
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var estado = await Permissions.CheckStatusAsync<TPermiso>();
                if (estado != PermissionStatus.Granted)
                {
                    estado = await Permissions.RequestAsync<TPermiso>();
                }

                return estado switch
                {
                    PermissionStatus.Granted or PermissionStatus.Limited => EstadoPermiso.Concedido,
                    PermissionStatus.Restricted => EstadoPermiso.NoDisponible,
                    _ => EstadoPermiso.Denegado
                };
            }
            catch (FeatureNotSupportedException)
            {
                return EstadoPermiso.NoDisponible;
            }
            catch (PermissionException)
            {
                // El permiso no está declarado en el manifiesto: es un error
                // de configuración, pero para el usuario equivale a no tenerlo.
                return EstadoPermiso.NoDisponible;
            }
        });
}
