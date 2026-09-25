namespace PerfilAlumno.Models;

/// <summary>
/// MODEL: datos puros del perfil, sin logica de presentacion.
/// </summary>
public class UserProfile
{
    public string Nombre { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Imagen local de Resources/Images o una URL completa.</summary>
    public string ImagenUrl { get; set; } = "dotnet_bot.png";
}
