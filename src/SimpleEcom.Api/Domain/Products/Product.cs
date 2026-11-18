using SimpleEcom.Api.Domain.Products.Events;
using SimpleEcom.Shared.Domain;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Domain.Products;

/// <summary>Agregado Producto: protege sus invariantes (precio, stock, SKU).</summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private Product(ProductId id, string sku, string name, string? description, Money price, int stock)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        IsActive = true;
    }

    /// <summary>Constructor para EF Core; no usar desde código de aplicación.</summary>
    private Product() : base(default)
    {
        Sku = null!;
        Name = null!;
        Price = null!;
    }

    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }

    public static Result<Product> Create(string sku, string name, string? description, Money price, int stock, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return ProductErrors.SkuRequired;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameRequired;
        }

        if (stock < 0)
        {
            return ProductErrors.NegativeStock;
        }

        var product = new Product(ProductId.New(), NormalizeSku(sku), name.Trim(), NormalizeDescription(description), price, stock);
        product.Raise(new ProductCreatedDomainEvent(product.Id, timeProvider.GetUtcNow()));
        return product;
    }

    public static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    /// <summary>
    /// Actualiza los datos editables de forma atómica: o se aplican todos o ninguno.
    /// El SKU es inmutable una vez creado.
    /// </summary>
    public Result Update(string name, string? description, Money price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(ProductErrors.NameRequired);
        }

        if (stock < 0)
        {
            return Result.Failure(ProductErrors.NegativeStock);
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        Price = price;
        Stock = stock;
        return Result.Success();
    }

    public void ChangePrice(Money newPrice) => Price = newPrice;

    public Result DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(ProductErrors.InvalidQuantity);
        }

        if (quantity > Stock)
        {
            return Result.Failure(ProductErrors.InsufficientStock);
        }

        Stock -= quantity;
        return Result.Success();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
