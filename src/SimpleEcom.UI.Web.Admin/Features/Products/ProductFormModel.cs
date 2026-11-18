using System.ComponentModel.DataAnnotations;
using SimpleEcom.Shared.Contracts;
using SimpleEcom.Shared.Contracts.Products;

namespace SimpleEcom.UI.Web.Admin.Features.Products;

/// <summary>
/// Modelo del formulario de producto. Solo valida en la UI para dar feedback inmediato;
/// las reglas de negocio reales las aplica el dominio en la Api.
/// </summary>
public sealed class ProductFormModel
{
    [Required(ErrorMessage = "El SKU es obligatorio.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "El precio no puede ser negativo.")]
    public decimal Price { get; set; }

    [EnumDataType(typeof(Currency), ErrorMessage = "La moneda indicada no está admitida.")]
    public Currency Currency { get; set; } = Currency.USD;

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    public static ProductFormModel FromResponse(ProductResponse product) => new()
    {
        Sku = product.Sku,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Currency = product.Currency,
        Stock = product.Stock
    };

    public CreateProductRequest ToCreateRequest() => new(Sku, Name, Description, Price, Currency, Stock);

    public UpdateProductRequest ToUpdateRequest() => new(Name, Description, Price, Currency, Stock);
}
