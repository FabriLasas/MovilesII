using System.Globalization;

namespace ExploradorPersonajes.Modelos;

/// <summary>Latitud y longitud en grados decimales.</summary>
public record Coordenadas(double Latitud, double Longitud)
{
    /// <summary>Cinco decimales equivalen a una precisión de alrededor de un metro.</summary>
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Latitud:F5}, {Longitud:F5}");
}
