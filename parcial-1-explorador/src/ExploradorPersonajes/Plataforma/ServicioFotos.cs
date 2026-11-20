using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Plataforma;

/// <summary>
/// Cámara y galería con MediaPicker.
///
/// Carpetas:
/// - Temporal (en la caché del sistema): fotos de un formulario todavía sin
///   guardar. Se vacía al iniciar la app, y Android la puede vaciar solo si
///   falta espacio.
/// - Definitiva (en los datos de la app): fotos de avistamientos guardados.
///   Sólo se borran al eliminar el avistamiento o al desinstalar la app.
/// </summary>
public class ServicioFotos : IServicioFotos
{
    /// <summary>
    /// Se pide al sistema que reduzca y comprima la foto: una foto de cámara
    /// pesa varios megabytes y para identificar un avistamiento alcanza con
    /// 1280 píxeles. Ahorra espacio y hace que la lista cargue rápido.
    /// </summary>
    private static readonly MediaPickerOptions Opciones = new()
    {
        Title = "Foto del avistamiento",
        MaximumWidth = 1280,
        MaximumHeight = 1280,
        CompressionQuality = 80,
        RotateImage = true,
        SelectionLimit = 1
    };

    private readonly IMediaPicker _mediaPicker;
    private readonly string _carpetaTemporal;
    private readonly string _carpetaDefinitiva;

    public ServicioFotos(IMediaPicker mediaPicker, string carpetaCache, string carpetaDatos)
    {
        _mediaPicker = mediaPicker;
        _carpetaTemporal = Path.Combine(carpetaCache, "fotos-pendientes");
        _carpetaDefinitiva = Path.Combine(carpetaDatos, "fotos");

        LimpiarTemporales();
    }

    public bool CamaraDisponible => _mediaPicker.IsCaptureSupported;

    public async Task<string?> TomarAsync()
        => await GuardarTemporalAsync(await _mediaPicker.CapturePhotoAsync(Opciones));

    /// <summary>
    /// PickPhotosAsync reemplaza en .NET 10 al PickPhotoAsync de una sola
    /// foto; con SelectionLimit = 1 el selector deja elegir una.
    /// </summary>
    public async Task<string?> ElegirDeGaleriaAsync()
    {
        var elegidas = await _mediaPicker.PickPhotosAsync(Opciones);
        return await GuardarTemporalAsync(elegidas?.FirstOrDefault());
    }

    public async Task<string> ConservarAsync(string rutaTemporal)
    {
        Directory.CreateDirectory(_carpetaDefinitiva);
        var destino = Path.Combine(_carpetaDefinitiva, Path.GetFileName(rutaTemporal));

        await using var origen = File.OpenRead(rutaTemporal);
        await using var archivo = File.Create(destino);
        await origen.CopyToAsync(archivo);

        return destino;
    }

    public void Eliminar(string ruta)
    {
        // Sólo se borran archivos de las carpetas propias: nunca una foto de
        // la galería del usuario aunque llegara una ruta equivocada.
        if (!EstaEn(ruta, _carpetaTemporal) && !EstaEn(ruta, _carpetaDefinitiva))
        {
            return;
        }

        try
        {
            File.Delete(ruta);
        }
        catch (IOException)
        {
            // Si no se puede borrar ahora, queda un archivo huérfano: no
            // justifica interrumpir al usuario.
        }
    }

    /// <summary>
    /// El archivo que entrega MediaPicker puede desaparecer en cualquier
    /// momento: se copia a la carpeta temporal propia con un nombre único.
    /// </summary>
    private async Task<string?> GuardarTemporalAsync(FileResult? resultado)
    {
        if (resultado is null)
        {
            return null;
        }

        Directory.CreateDirectory(_carpetaTemporal);
        var extension = Path.GetExtension(resultado.FileName);
        var destino = Path.Combine(_carpetaTemporal, $"{Guid.NewGuid():N}{(string.IsNullOrEmpty(extension) ? ".jpg" : extension)}");

        await using var origen = await resultado.OpenReadAsync();
        await using var archivo = File.Create(destino);
        await origen.CopyToAsync(archivo);

        return destino;
    }

    /// <summary>
    /// Al iniciar no hay ningún formulario abierto: cualquier foto temporal
    /// es de un formulario abandonado.
    /// </summary>
    private void LimpiarTemporales()
    {
        try
        {
            if (Directory.Exists(_carpetaTemporal))
            {
                Directory.Delete(_carpetaTemporal, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    private static bool EstaEn(string ruta, string carpeta)
        => !string.IsNullOrEmpty(ruta)
           && Path.GetFullPath(ruta).StartsWith(Path.GetFullPath(carpeta) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
