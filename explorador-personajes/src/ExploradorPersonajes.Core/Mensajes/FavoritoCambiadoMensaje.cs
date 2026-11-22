using ExploradorPersonajes.Modelos;

namespace ExploradorPersonajes.Mensajes;

/// <summary>
/// Aviso de que un personaje se agregó o se quitó de favoritos.
///
/// Un mismo favorito se ve en tres lugares a la vez: la lista, la pestaña de
/// favoritos y el detalle. En lugar de que cada ViewModel conozca a los demás,
/// quien cambia un favorito publica este mensaje y cada pantalla se actualiza
/// por su cuenta. Lleva el personaje completo para que la pestaña de
/// favoritos pueda agregarlo sin volver a pedirlo a la API.
/// </summary>
public record FavoritoCambiadoMensaje(Personaje Personaje, bool EsFavorito);
