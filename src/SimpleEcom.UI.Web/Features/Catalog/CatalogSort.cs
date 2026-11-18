using SimpleEcom.Shared.Contracts.Products;

namespace SimpleEcom.UI.Web.Features.Catalog;

/// <summary>Órdenes del catálogo. Los valores de <see cref="Options"/> son los que viajan en ?sort= de la URL.</summary>
public static class CatalogSort
{
    public const string Default = "";
    public const string PriceAsc = "price-asc";
    public const string PriceDesc = "price-desc";
    public const string Name = "name";

    public static IReadOnlyList<(string Value, string Label)> Options { get; } =
    [
        (Default, "Destacados"),
        (PriceAsc, "Precio: de menor a mayor"),
        (PriceDesc, "Precio: de mayor a menor"),
        (Name, "Nombre (A-Z)")
    ];

    /// <summary>Normaliza un valor recibido por URL; cualquier valor desconocido vuelve al orden por defecto.</summary>
    public static string Normalize(string? value) =>
        Options.Any(o => o.Value == value) ? value! : Default;

    /// <summary>
    /// Aplica el orden. En "Destacados" se mantiene el orden de la Api pero los agotados van al final,
    /// para que el comprador vea primero lo que puede comprar.
    /// </summary>
    public static IEnumerable<ProductResponse> Apply(IEnumerable<ProductResponse> products, string sort) => sort switch
    {
        PriceAsc => products.OrderBy(p => p.Price).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
        PriceDesc => products.OrderByDescending(p => p.Price).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
        Name => products.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
        _ => products.OrderBy(p => p.Stock <= 0)
    };
}
