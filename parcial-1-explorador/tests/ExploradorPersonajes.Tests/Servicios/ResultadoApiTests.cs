using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Tests.Servicios;

public class ResultadoApiTests
{
    [Fact]
    public void Transformar_ResultadoDesdeCache_ConservaElOrigenYLaFecha()
    {
        var fecha = new DateTime(2026, 11, 1);
        var original = ResultadoApi<int>.ExitoDesdeCache(5, fecha, TipoError.SinConexion, "sin red");

        var transformado = original.Transformar(n => new List<int> { n });

        Assert.True(transformado.EsExitoso);
        Assert.True(transformado.DesdeCache);
        Assert.Equal(fecha, transformado.FechaCache);
        Assert.Equal(TipoError.SinConexion, transformado.Error);
        Assert.Equal("sin red", transformado.Mensaje);
        Assert.Equal(new[] { 5 }, transformado.Datos);
    }

    [Fact]
    public void Transformar_ResultadoFallido_NoEjecutaLaConversionYConservaElError()
    {
        var original = ResultadoApi<int>.Fallo(TipoError.ErrorServidor, "falló", 500);
        var seLlamo = false;

        var transformado = original.Transformar(n => { seLlamo = true; return n.ToString(); });

        Assert.False(seLlamo);
        Assert.False(transformado.EsExitoso);
        Assert.Null(transformado.Datos);
        Assert.Equal(500, transformado.CodigoHttp);
    }
}
