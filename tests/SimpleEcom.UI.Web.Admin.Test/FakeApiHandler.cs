using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SimpleEcom.Shared.Contracts;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Http;

namespace SimpleEcom.UI.Web.Admin.Test;

/// <summary>
/// Simula la Api a nivel HTTP: se registran rutas ("GET /api/products") con la respuesta a devolver.
/// Así los ViewModels se prueban con el ProductsApiClient real, incluida la lectura de ProblemDetails.
/// </summary>
internal sealed class FakeApiHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new();

    /// <summary>Peticiones recibidas, en orden, como "METHOD /path?query".</summary>
    public List<string> Requests { get; } = [];

    public FakeApiHandler On(HttpMethod method, string pathAndQuery, HttpStatusCode status, object? body = null) =>
        On(method, pathAndQuery, _ => Response(status, body));

    public FakeApiHandler On(HttpMethod method, string pathAndQuery, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _routes[Key(method, pathAndQuery)] = respond;
        return this;
    }

    public ProductsApiClient CreateClient() =>
        new(new HttpClient(this) { BaseAddress = new Uri("https://api.test/") });

    public static HttpResponseMessage Response(HttpStatusCode status, object? body = null)
    {
        var response = new HttpResponseMessage(status);
        if (body is not null)
        {
            response.Content = JsonContent.Create(body, options: Json);
        }

        return response;
    }

    /// <summary>Respuesta ProblemDetails como la que genera la Api para errores de negocio.</summary>
    public static HttpResponseMessage Problem(HttpStatusCode status, string title, string detail) =>
        Response(status, new { title, detail, status = (int)status });

    public static ProductResponse Product(Guid? id = null, string sku = "SKU-1", string name = "Camiseta", bool isActive = true) =>
        new(id ?? Guid.NewGuid(), sku, name, null, 19.99m, Currency.USD, 10, isActive);

    public int CountRequests(HttpMethod method, string pathAndQuery) =>
        Requests.Count(r => r == Key(method, pathAndQuery));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = Key(request.Method, request.RequestUri!.PathAndQuery);
        Requests.Add(key);

        return Task.FromResult(_routes.TryGetValue(key, out var respond)
            ? respond(request)
            : Problem(HttpStatusCode.NotFound, "Fake.RouteNotFound", $"Ruta no simulada: {key}"));
    }

    private static string Key(HttpMethod method, string pathAndQuery) => $"{method} {pathAndQuery}";
}
