using System.Globalization;
using SimpleEcom.Shared.Contracts;

namespace SimpleEcom.UI.Web.Features.Catalog;

/// <summary>Disponibilidad de un producto tal y como la ve el comprador.</summary>
public enum StockStatus
{
    InStock,
    LowStock,
    OutOfStock
}

/// <summary>Reglas de presentación del catálogo: formato de precios, disponibilidad e imagen de reserva.</summary>
public static class CatalogDisplay
{
    /// <summary>Con este stock o menos se avisa de "últimas unidades".</summary>
    public const int LowStockThreshold = 5;

    /// <summary>Palabras que no aportan inicial ("Bolsa de tela" → "BT").</summary>
    private static readonly HashSet<string> ConnectorWords =
        new(["de", "del", "la", "las", "el", "los", "y", "con", "para", "en", "a"], StringComparer.OrdinalIgnoreCase);

    private static readonly CultureInfo UsdCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo EurCulture = CultureInfo.GetCultureInfo("es-ES");

    public static string FormatPrice(decimal price, Currency currency) => currency switch
    {
        Currency.USD => price.ToString("C2", UsdCulture),
        Currency.EUR => price.ToString("C2", EurCulture),
        _ => $"{price:N2} {currency}"
    };

    public static StockStatus GetStockStatus(int stock) => stock switch
    {
        <= 0 => StockStatus.OutOfStock,
        <= LowStockThreshold => StockStatus.LowStock,
        _ => StockStatus.InStock
    };

    public static string StockLabel(int stock) => GetStockStatus(stock) switch
    {
        StockStatus.OutOfStock => "Agotado",
        StockStatus.LowStock => stock == 1 ? "¡Última unidad!" : $"¡Últimas {stock} unidades!",
        _ => "En stock"
    };

    /// <summary>Iniciales del nombre (máx. 2) para la imagen de reserva, mientras los productos no tengan foto.</summary>
    public static string Initials(string name)
    {
        var letters = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => char.IsLetterOrDigit(w[0]) && !ConnectorWords.Contains(w))
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0]));

        var initials = string.Concat(letters);
        return initials.Length == 0 ? "?" : initials;
    }

    /// <summary>Tono (0-359) estable a partir del SKU: el mismo producto siempre tiene el mismo color.</summary>
    /// <remarks>No se usa string.GetHashCode porque cambia entre ejecuciones.</remarks>
    public static int Hue(string sku)
    {
        var hash = 0;
        foreach (var c in sku)
        {
            hash = unchecked(hash * 31 + c);
        }

        return (int)((uint)hash % 360);
    }
}
