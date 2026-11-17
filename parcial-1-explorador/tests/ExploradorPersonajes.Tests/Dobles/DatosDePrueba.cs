using System.Text.Json;
using ExploradorPersonajes.Modelos;
using ExploradorPersonajes.Servicios;

namespace ExploradorPersonajes.Tests.Dobles;

/// <summary>
/// Datos de ejemplo con la misma forma que devuelve la API real. Centralizarlos
/// evita repetir el JSON en cada prueba y que una prueba falle por un error de
/// tipeo en los datos y no en el código.
/// </summary>
public static class DatosDePrueba
{
    public static Personaje Personaje(int id, string nombre = "Personaje", string estado = "Alive") => new()
    {
        Id = id,
        Nombre = nombre,
        Estado = estado,
        Especie = "Human",
        Genero = "Male",
        ImagenUrl = $"https://rickandmortyapi.com/api/character/avatar/{id}.jpeg",
        Origen = new Ubicacion { Nombre = "Earth (C-137)" },
        UbicacionActual = new Ubicacion { Nombre = "Citadel of Ricks" }
    };

    /// <summary>JSON de un personaje con los nombres de campo en inglés de la API.</summary>
    public static string JsonPersonaje(int id, string nombre = "Personaje") => $$"""
        {
          "id": {{id}},
          "name": "{{nombre}}",
          "status": "Alive",
          "species": "Human",
          "gender": "Male",
          "image": "https://rickandmortyapi.com/api/character/avatar/{{id}}.jpeg",
          "origin": { "name": "Earth (C-137)", "url": "" },
          "location": { "name": "Citadel of Ricks", "url": "" }
        }
        """;

    public static string JsonLista(params int[] ids)
        => "[" + string.Join(",", ids.Select(id => JsonPersonaje(id, $"Personaje {id}"))) + "]";

    public static string JsonPagina(int total, bool haySiguiente, params int[] ids) => $$"""
        {
          "info": {
            "count": {{total}},
            "pages": 42,
            "next": {{(haySiguiente ? "\"https://rickandmortyapi.com/api/character/?page=2\"" : "null")}},
            "prev": null
          },
          "results": [{{string.Join(",", ids.Select(id => JsonPersonaje(id, $"Personaje {id}")))}}]
        }
        """;

    public static RespuestaPaginada Pagina(int total, bool haySiguiente, params int[] ids)
        => JsonSerializer.Deserialize<RespuestaPaginada>(JsonPagina(total, haySiguiente, ids))!;

    public static ResultadoApi<RespuestaPaginada> PaginaExitosa(int total, bool haySiguiente, params int[] ids)
        => ResultadoApi<RespuestaPaginada>.Exito(Pagina(total, haySiguiente, ids));
}
