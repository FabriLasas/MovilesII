namespace PerfilAlumno.Services;

/// <summary>
/// Abstrae la navegacion para que los ViewModels NO dependan de Shell
/// ni de ningun tipo de la UI. Esto mantiene el ViewModel testeable
/// y concentra toda la navegacion en un unico lugar.
/// </summary>
public interface INavigationService
{
    /// <summary>Navega a una ruta registrada, opcionalmente con parametros.</summary>
    /// <returns>true si la navegacion se realizo; false si fallo la validacion.</returns>
    Task<bool> IrAAsync(string ruta, IDictionary<string, object>? parametros = null);

    /// <summary>Vuelve a la pagina anterior de la pila.</summary>
    Task VolverAsync();

    /// <summary>Ultimo error de navegacion (vacio si la ultima llamada fue exitosa).</summary>
    string UltimoError { get; }
}
