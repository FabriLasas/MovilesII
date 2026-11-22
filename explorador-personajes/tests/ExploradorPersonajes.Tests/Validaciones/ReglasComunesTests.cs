using ExploradorPersonajes.Validaciones;

namespace ExploradorPersonajes.Tests.Validaciones;

public class ReglasComunesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Obligatorio_TextoVacio_DevuelveError(string? valor)
    {
        Assert.Equal("Completá la nota.", ReglasComunes.Obligatorio(valor, "la nota"));
    }

    [Fact]
    public void Obligatorio_ConTexto_NoDevuelveError()
    {
        Assert.Null(ReglasComunes.Obligatorio("hola", "la nota"));
    }

    [Theory]
    [InlineData("ab", false)]
    [InlineData("abc", true)]
    [InlineData("  ab  ", false)] // los espacios de los extremos no cuentan
    [InlineData("abcde", true)]
    [InlineData("abcdef", false)]
    public void Largo_RespetaMinimoYMaximoSinContarEspaciosExtremos(string valor, bool esValido)
    {
        var error = ReglasComunes.Largo(valor, 3, 5, "la nota");

        Assert.Equal(esValido, error is null);
        if (!esValido)
        {
            Assert.Equal("La nota debe tener entre 3 y 5 caracteres.", error);
        }
    }

    [Fact]
    public void LargoMaximo_TextoMasLargo_DevuelveErrorConElLimite()
    {
        Assert.Equal("La búsqueda no puede superar los 5 caracteres.",
            ReglasComunes.LargoMaximo("abcdef", 5, "la búsqueda"));
        Assert.Null(ReglasComunes.LargoMaximo("abcde", 5, "la búsqueda"));
        Assert.Null(ReglasComunes.LargoMaximo(null, 5, "la búsqueda"));
    }

    [Theory]
    [InlineData(-90.0, true)]
    [InlineData(90.0, true)]
    [InlineData(-34.6037, true)]
    [InlineData(90.0001, false)]
    [InlineData(-91.0, false)]
    [InlineData(double.NaN, false)]
    public void Latitud_ValidaElRango(double valor, bool esValido)
    {
        Assert.Equal(esValido, ReglasComunes.Latitud(valor) is null);
    }

    [Theory]
    [InlineData(-180.0, true)]
    [InlineData(180.0, true)]
    [InlineData(-58.3816, true)]
    [InlineData(180.5, false)]
    [InlineData(double.NaN, false)]
    public void Longitud_ValidaElRango(double valor, bool esValido)
    {
        Assert.Equal(esValido, ReglasComunes.Longitud(valor) is null);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void IdPositivo_RechazaCeroYNegativos(int id, bool esValido)
    {
        Assert.Equal(esValido, ReglasComunes.IdPositivo(id, "el personaje") is null);
    }
}
