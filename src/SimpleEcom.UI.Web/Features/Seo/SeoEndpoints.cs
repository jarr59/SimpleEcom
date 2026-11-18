using System.Text;
using System.Xml;
using SimpleEcom.Shared.Http;

namespace SimpleEcom.UI.Web.Features.Seo;

/// <summary><c>/robots.txt</c> y <c>/sitemap.xml</c> para que los buscadores descubran las páginas de la tienda.</summary>
public static class SeoEndpoints
{
    /// <summary>Páginas fijas de la tienda que deben indexarse.</summary>
    public static readonly IReadOnlyList<string> StaticPaths = ["", "products", "branches"];

    public static IEndpointRouteBuilder MapSeo(this IEndpointRouteBuilder app)
    {
        app.MapGet("robots.txt", (HttpRequest request) =>
                Results.Text(RobotsTxt(BaseUrl(request)), "text/plain; charset=utf-8"))
            .ExcludeFromDescription();

        app.MapGet("sitemap.xml", async (HttpRequest request, ProductsApiClient api, CancellationToken ct) =>
            {
                var products = await api.GetProductsAsync(isActive: true, cancellationToken: ct);
                if (products.IsFailure)
                {
                    // 503: el buscador reintenta más tarde en vez de quedarse con un sitemap vacío.
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                var paths = StaticPaths.Concat(products.Value.Where(p => p.IsActive).Select(p => $"products/{p.Id}"));
                return Results.Text(SitemapXml(BaseUrl(request), paths), "application/xml; charset=utf-8");
            })
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>Base absoluta del sitio (esquema + host + PathBase) terminada en "/".</summary>
    public static string BaseUrl(HttpRequest request) =>
        $"{request.Scheme}://{request.Host}{request.PathBase}/";

    public static string RobotsTxt(string baseUrl) =>
        $"""
        User-agent: *
        Allow: /
        Disallow: /branches/select

        Sitemap: {baseUrl}sitemap.xml

        """;

    /// <summary>Sitemap según https://www.sitemaps.org/protocol.html; las URLs se escapan como XML.</summary>
    public static string SitemapXml(string baseUrl, IEnumerable<string> relativePaths)
    {
        const string Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var builder = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };

        using (var writer = XmlWriter.Create(new StringWriterUtf8(builder), settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", Ns);
            foreach (var path in relativePaths.Distinct())
            {
                writer.WriteStartElement("url", Ns);
                writer.WriteElementString("loc", Ns, baseUrl + path);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return builder.ToString();
    }

    /// <summary>StringWriter que declara UTF-8 (el de serie declara UTF-16 en la cabecera XML).</summary>
    private sealed class StringWriterUtf8(StringBuilder builder) : StringWriter(builder)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
