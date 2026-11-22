using System.Net;
using System.Text;

namespace ExploradorPersonajes.Tests.Dobles;

/// <summary>
/// Reemplaza la red en las pruebas del servicio. HttpClient delega el envío
/// en un HttpMessageHandler: pasándole este, cada petición se responde con lo
/// que la prueba configuró, sin salir a internet. Así las pruebas son rápidas,
/// no dependen de que la API esté en línea y pueden simular errores (un 500,
/// un timeout) que en la API real no se pueden provocar a voluntad.
/// </summary>
public class ManejadorHttpFalso : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    private ManejadorHttpFalso(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Rutas pedidas, relativas a la URL base de la API.</summary>
    public List<string> RutasPedidas { get; } = new();

    public static ManejadorHttpFalso QueResponda(HttpStatusCode codigo, string cuerpo = "")
        => new(_ => new HttpResponseMessage(codigo)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
        });

    public static ManejadorHttpFalso QueLance(Exception excepcion)
        => new(_ => throw excepcion);

    /// <summary>Permite responder distinto según la ruta pedida.</summary>
    public static ManejadorHttpFalso ConRespuestas(Func<string, (HttpStatusCode Codigo, string Cuerpo)> segunRuta)
        => new(peticion =>
        {
            var (codigo, cuerpo) = segunRuta(peticion.RequestUri!.PathAndQuery.Replace("/api/", ""));
            return new HttpResponseMessage(codigo)
            {
                Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
            };
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RutasPedidas.Add(request.RequestUri!.PathAndQuery.Replace("/api/", ""));
        return Task.FromResult(_responder(request));
    }
}
