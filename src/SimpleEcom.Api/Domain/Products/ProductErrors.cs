using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NameRequired = Error.Validation("Product.NameRequired", "El nombre del producto es obligatorio.");
    public static readonly Error SkuRequired = Error.Validation("Product.SkuRequired", "El SKU del producto es obligatorio.");
    public static readonly Error NegativePrice = Error.Validation("Product.NegativePrice", "El precio no puede ser negativo.");
    public static readonly Error InvalidCurrency = Error.Validation("Product.InvalidCurrency", "La moneda indicada no está admitida.");
    public static readonly Error NegativeStock = Error.Validation("Product.NegativeStock", "El stock no puede ser negativo.");
    public static readonly Error InvalidQuantity = Error.Validation("Product.InvalidQuantity", "La cantidad debe ser mayor que cero.");
    public static readonly Error InsufficientStock = Error.Conflict("Product.InsufficientStock", "No hay stock suficiente.");

    public static Error NotFound(ProductId id) => Error.NotFound("Product.NotFound", $"No existe el producto '{id.Value}'.");
    public static Error SkuAlreadyExists(string sku) => Error.Conflict("Product.SkuAlreadyExists", $"Ya existe un producto con SKU '{sku}'.");
}
