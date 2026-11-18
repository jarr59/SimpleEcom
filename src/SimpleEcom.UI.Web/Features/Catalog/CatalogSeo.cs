using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleEcom.Shared.Contracts.Products;

namespace SimpleEcom.UI.Web.Features.Catalog;

/// <summary>Reglas de SEO del catálogo: meta descripciones y datos estructurados (schema.org) de producto.</summary>
public static partial class CatalogSeo
{
    /// <summary>Los buscadores suelen cortar la descripción hacia los 155-160 caracteres.</summary>
    public const int MaxDescriptionLength = 160;

    public const string StoreName = "SimpleEcom";

    /// <summary>
    /// Texto para <c>meta description</c>: espacios normalizados y recortado en un límite de palabra con "…".
    /// Si el texto está vacío se usa <paramref name="fallback"/>.
    /// </summary>
    public static string MetaDescription(string? text, string fallback)
    {
        var clean = string.IsNullOrWhiteSpace(text) ? fallback : Whitespace().Replace(text.Trim(), " ");
        if (clean.Length <= MaxDescriptionLength)
        {
            return clean;
        }

        var cut = clean[..(MaxDescriptionLength - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > MaxDescriptionLength / 2 ? cut[..lastSpace] : cut).TrimEnd(',', '.', ';', ':') + "…";
    }

    public static string ProductDescription(ProductResponse product) =>
        MetaDescription(product.Description, $"{product.Name} en {StoreName}. Ref. {product.Sku}.");

    /// <summary>
    /// JSON-LD <c>Product</c> con su oferta (precio, moneda y disponibilidad) para los resultados enriquecidos.
    /// System.Text.Json escapa por defecto &lt;, &gt; y &amp;, así que el texto del producto no puede cerrar el &lt;script&gt;.
    /// </summary>
    public static string ProductJsonLd(ProductResponse product, string absoluteUrl)
    {
        var data = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["sku"] = product.Sku,
            ["description"] = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description.Trim(),
            ["url"] = absoluteUrl,
            ["brand"] = new Dictionary<string, object> { ["@type"] = "Brand", ["name"] = StoreName },
            ["offers"] = new Dictionary<string, object>
            {
                ["@type"] = "Offer",
                ["url"] = absoluteUrl,
                ["price"] = product.Price,
                ["priceCurrency"] = product.Currency.ToString(),
                ["availability"] = product.Stock > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
                ["itemCondition"] = "https://schema.org/NewCondition"
            }
        };

        // Sin descripción se omite la clave en vez de enviar null.
        if (data["description"] is null)
        {
            data.Remove("description");
        }

        return JsonSerializer.Serialize(data);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
