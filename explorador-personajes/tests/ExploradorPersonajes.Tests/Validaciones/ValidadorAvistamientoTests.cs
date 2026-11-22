using ExploradorPersonajes.Datos;
using ExploradorPersonajes.Validaciones;

namespace ExploradorPersonajes.Tests.Validaciones;

public class ValidadorAvistamientoTests
{
    /// <summary>Un avistamiento que cumple todas las reglas; cada prueba rompe una.</summary>
    private static Avistamiento Valido() => new()
    {
        PersonajeId = 1,
        RutaFoto = "/data/fotos/abc.jpg",
        Nota = "Lo vi en la plaza",
        Latitud = -34.6037,
        Longitud = -58.3816,
        Fecha = new DateTime(2026, 11, 19)
    };

    [Fact]
    public void Validar_AvistamientoCompleto_EsValido()
    {
        var resultado = ValidadorAvistamiento.Validar(Valido());

        Assert.True(resultado.EsValido);
        Assert.Empty(resultado.Errores);
    }

    [Fact]
    public void Validar_SinUbicacion_EsValidoPorqueEsOpcional()
    {
        var avistamiento = Valido();
        avistamiento.Latitud = null;
        avistamiento.Longitud = null;

        Assert.True(ValidadorAvistamiento.Validar(avistamiento).EsValido);
    }

    [Fact]
    public void Validar_SinFoto_InformaElErrorEnElCampoFoto()
    {
        var avistamiento = Valido();
        avistamiento.RutaFoto = "";

        var resultado = ValidadorAvistamiento.Validar(avistamiento);

        Assert.Equal("Sacá una foto o elegila de la galería.", resultado.ErrorDe(ValidadorAvistamiento.CampoFoto));
    }

    [Fact]
    public void Validar_NotaVacia_PideCompletarlaYNoRepiteElErrorDeLargo()
    {
        var avistamiento = Valido();
        avistamiento.Nota = "   ";

        var resultado = ValidadorAvistamiento.Validar(avistamiento);

        Assert.Equal("Completá la nota.", resultado.ErrorDe(ValidadorAvistamiento.CampoNota));
        Assert.Single(resultado.Errores);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validar_LargoDeLaNota(int largo, bool esValido)
    {
        var avistamiento = Valido();
        avistamiento.Nota = new string('a', largo);

        var resultado = ValidadorAvistamiento.Validar(avistamiento);

        Assert.Equal(esValido, resultado.EsValido);
        if (!esValido)
        {
            Assert.Equal("La nota debe tener entre 3 y 200 caracteres.", resultado.ErrorDe(ValidadorAvistamiento.CampoNota));
        }
    }

    [Fact]
    public void Validar_UbicacionIncompleta_EsInvalida()
    {
        var avistamiento = Valido();
        avistamiento.Longitud = null;

        var resultado = ValidadorAvistamiento.Validar(avistamiento);

        Assert.Equal("La ubicación está incompleta. Volvé a obtenerla o quitala.",
            resultado.ErrorDe(ValidadorAvistamiento.CampoUbicacion));
    }

    [Theory]
    [InlineData(95.0, -58.0)]
    [InlineData(-34.0, 200.0)]
    public void Validar_CoordenadasFueraDeRango_SonInvalidas(double latitud, double longitud)
    {
        var avistamiento = Valido();
        avistamiento.Latitud = latitud;
        avistamiento.Longitud = longitud;

        var resultado = ValidadorAvistamiento.Validar(avistamiento);

        Assert.NotNull(resultado.ErrorDe(ValidadorAvistamiento.CampoUbicacion));
    }

    [Fact]
    public void Validar_SinPersonaje_EsInvalido()
    {
        var avistamiento = Valido();
        avistamiento.PersonajeId = 0;

        Assert.NotNull(ValidadorAvistamiento.Validar(avistamiento).ErrorDe(ValidadorAvistamiento.CampoPersonaje));
    }

    [Fact]
    public void Validar_VariosProblemas_LosInformaTodosJuntos()
    {
        var avistamiento = new Avistamiento { PersonajeId = 1, Nota = "a", Latitud = 100, Longitud = 0 };

        var resultado = ValidadorAvistamiento.Validar(avistamiento);

        Assert.Equal(3, resultado.Errores.Count);
        Assert.NotNull(resultado.ErrorDe(ValidadorAvistamiento.CampoFoto));
        Assert.NotNull(resultado.ErrorDe(ValidadorAvistamiento.CampoNota));
        Assert.NotNull(resultado.ErrorDe(ValidadorAvistamiento.CampoUbicacion));
    }
}
