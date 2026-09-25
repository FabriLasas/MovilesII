using PerfilAlumno.Views;

namespace PerfilAlumno.Navigation;

/// <summary>
/// Rutas de la app en un solo lugar. Usar constantes en vez de strings
/// sueltos evita errores de tipeo que solo se descubren en runtime.
/// </summary>
public static class Routes
{
    public const string Detalle = "detalle";

    /// <summary>
    /// Registra las rutas que NO estan declaradas como ShellContent
    /// en AppShell.xaml. Llamar una sola vez al iniciar la app.
    /// </summary>
    public static void Registrar()
    {
        Routing.RegisterRoute(Detalle, typeof(ProfileDetailPage));
    }
}

/// <summary>
/// Nombres de los parametros que viajan entre paginas.
/// El emisor y el receptor usan las mismas constantes, asi no se
/// desincronizan nunca.
/// </summary>
public static class ParametrosPerfil
{
    public const string Nombre = "nombre";
    public const string Edad = "edad";
    public const string Descripcion = "descripcion";
    public const string Imagen = "imagen";
}
