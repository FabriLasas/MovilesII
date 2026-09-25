namespace PerfilAlumno.Services;

/// <summary>
/// Implementacion concreta sobre Shell.Current.GoToAsync().
/// Es la UNICA clase de toda la app que conoce a Shell: si maniana
/// cambiamos de motor de navegacion, solo se toca este archivo.
/// </summary>
public class NavigationService : INavigationService
{
    public string UltimoError { get; private set; } = string.Empty;

    public async Task<bool> IrAAsync(string ruta, IDictionary<string, object>? parametros = null)
    {
        UltimoError = string.Empty;

        // --- Validacion 1: la ruta tiene que existir ---
        if (string.IsNullOrWhiteSpace(ruta))
        {
            UltimoError = "La ruta de destino esta vacia.";
            return false;
        }

        // --- Validacion 2: ningun parametro puede tener clave vacia o valor nulo ---
        if (parametros is not null)
        {
            foreach (var par in parametros)
            {
                if (string.IsNullOrWhiteSpace(par.Key))
                {
                    UltimoError = "Hay un parametro sin nombre.";
                    return false;
                }

                if (par.Value is null)
                {
                    UltimoError = $"El parametro '{par.Key}' llego nulo.";
                    return false;
                }
            }
        }

        // --- Navegacion propiamente dicha ---
        try
        {
            if (parametros is null)
                await Shell.Current.GoToAsync(ruta);
            else
                await Shell.Current.GoToAsync(ruta, parametros);

            return true;
        }
        catch (Exception ex)
        {
            // Tipico si la ruta no fue registrada con Routing.RegisterRoute.
            UltimoError = $"No se pudo navegar a '{ruta}': {ex.Message}";
            return false;
        }
    }

    public Task VolverAsync() => Shell.Current.GoToAsync("..");
}
