using System.Text.Json;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Tests.Dobles;

namespace ExploradorPersonajes.Tests.Modelos;

public class PersonajeTests
{
    [Theory]
    [InlineData("Alive", "Vivo", "#2E9E5B")]
    [InlineData("Dead", "Muerto", "#C0392B")]
    [InlineData("unknown", "Desconocido", "#7F8C8D")]
    [InlineData("", "Desconocido", "#7F8C8D")]
    public void EstadoTraducido_SegunElEstadoDeLaApi_DevuelveTextoYColor(string estado, string texto, string color)
    {
        var personaje = DatosDePrueba.Personaje(1, estado: estado);

        Assert.Equal(texto, personaje.EstadoTraducido);
        Assert.Equal(color, personaje.ColorEstado);
    }

    [Fact]
    public void Subtitulo_CombinaEstadoYEspecie()
    {
        Assert.Equal("Vivo · Human", DatosDePrueba.Personaje(1).Subtitulo);
    }

    [Fact]
    public void Deserializar_JsonDeLaApi_MapeaLosCamposEnInglesALasPropiedades()
    {
        var personaje = JsonSerializer.Deserialize<Personaje>(DatosDePrueba.JsonPersonaje(1, "Rick Sanchez"))!;

        Assert.Equal(1, personaje.Id);
        Assert.Equal("Rick Sanchez", personaje.Nombre);
        Assert.Equal("Alive", personaje.Estado);
        Assert.Equal("Human", personaje.Especie);
        Assert.Equal("Male", personaje.Genero);
        Assert.Equal("Earth (C-137)", personaje.Origen!.Nombre);
        Assert.Equal("Citadel of Ricks", personaje.UbicacionActual!.Nombre);
    }
}
